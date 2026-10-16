/* ============================================================================
   MIOS - Repair the database schema, add roles, and seed spare-parts data
   ----------------------------------------------------------------------------
   Background
   ----------
   The Inventory database was created from create_inventory_database.sql, and
   only part of the later upgrade scripts were ever applied. Because of that,
   these operations failed at runtime with "Invalid object name":

       * Financial reports   (mn.Account / mn.JournalEntry / mn.JournalLine)
       * Record expense      (mn.Account / mn.Expense)
       * Add spare part      (mn.PurchaseReceipt / mn.PurchaseReceiptLine)
       * Issue / sell part   (mn.SalesInvoiceLine)
       * Record payment      (mn.Payment / mn.PurchaseReceipt)
       * Adjust stock        (mn.InventoryAdjustment)
       * Audit log           (mn.AuditLog)

   This script:
       1. Adds the few columns mn.SalesInvoice is missing.
       2. Creates every missing table, matching exactly what the forms use.
       3. Seeds a chart of accounts.
       4. Adds the Clerk and Accountant roles with sensible permissions.
       5. Seeds categories, conditions, suppliers and a car spare-parts
          catalogue (with matching purchase batches and stock-card entries so
          the master quantity and the FIFO ledger stay consistent).

   It is idempotent - safe to run more than once.
   ============================================================================ */

USE [Inventory];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------------------------------------------------------
   1. mn.SalesInvoice - columns added in later versions
   --------------------------------------------------------------------------- */
IF COL_LENGTH(N'mn.SalesInvoice', N'Subtotal') IS NULL
    ALTER TABLE [mn].[SalesInvoice] ADD Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_Subtotal DEFAULT 0;
IF COL_LENGTH(N'mn.SalesInvoice', N'TaxAmount') IS NULL
    ALTER TABLE [mn].[SalesInvoice] ADD TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_TaxAmount DEFAULT 0;
IF COL_LENGTH(N'mn.SalesInvoice', N'AmountPaid') IS NULL
    ALTER TABLE [mn].[SalesInvoice] ADD AmountPaid DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_AmountPaid DEFAULT 0;
IF COL_LENGTH(N'mn.SalesInvoice', N'Remarks') IS NULL
    ALTER TABLE [mn].[SalesInvoice] ADD Remarks NVARCHAR(500) NULL;
GO

/* ---------------------------------------------------------------------------
   2. Accounting tables
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'[mn].[Account]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Account]
    (
        AccountID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AccountCode NVARCHAR(20)  NOT NULL UNIQUE,
        AccountName NVARCHAR(150) NOT NULL,
        AccountType NVARCHAR(30)  NOT NULL,
        IsActive    BIT NOT NULL CONSTRAINT DF_Account_IsActive DEFAULT 1
    );
END;
GO

IF OBJECT_ID(N'[mn].[JournalEntry]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[JournalEntry]
    (
        JournalEntryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EntryDate      DATETIME2 NOT NULL CONSTRAINT DF_JournalEntry_EntryDate DEFAULT SYSDATETIME(),
        ReferenceType  NVARCHAR(50)  NOT NULL,
        ReferenceID    INT NULL,
        Description    NVARCHAR(500) NOT NULL,
        CreatedByUserID INT NULL,
        CONSTRAINT FK_JournalEntry_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[JournalLine]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[JournalLine]
    (
        JournalLineID  INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        JournalEntryID INT NOT NULL,
        AccountID      INT NOT NULL,
        Debit          DECIMAL(18,2) NOT NULL CONSTRAINT CK_JournalLine_Debit  CHECK (Debit  >= 0),
        Credit         DECIMAL(18,2) NOT NULL CONSTRAINT CK_JournalLine_Credit CHECK (Credit >= 0),
        CONSTRAINT FK_JournalLine_Entry   FOREIGN KEY (JournalEntryID) REFERENCES [mn].[JournalEntry](JournalEntryID),
        CONSTRAINT FK_JournalLine_Account FOREIGN KEY (AccountID)      REFERENCES [mn].[Account](AccountID)
    );
END;
GO

/* ---------------------------------------------------------------------------
   3. Purchasing / sales / payment tables
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'[mn].[PurchaseReceipt]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[PurchaseReceipt]
    (
        ReceiptID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierID    INT NULL,
        ReceiptDate   DATETIME2 NOT NULL CONSTRAINT DF_PurchaseReceipt_ReceiptDate DEFAULT SYSDATETIME(),
        TotalAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseReceipt_Total DEFAULT 0,
        AmountPaid    DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseReceipt_AmountPaid DEFAULT 0,
        Status        NVARCHAR(20) NOT NULL CONSTRAINT DF_PurchaseReceipt_Status DEFAULT N'UNPAID',
        Notes         NVARCHAR(500) NULL,
        CreatedByUserID INT NULL,
        CONSTRAINT FK_PurchaseReceipt_Supplier FOREIGN KEY (SupplierID) REFERENCES [mn].[Supplier](SupplierID),
        CONSTRAINT FK_PurchaseReceipt_User     FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[PurchaseReceiptLine]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[PurchaseReceiptLine]
    (
        ReceiptLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReceiptID     INT NOT NULL,
        PartNumber    NVARCHAR(100) NOT NULL,
        Quantity      INT NOT NULL CONSTRAINT CK_PurchaseReceiptLine_Quantity CHECK (Quantity > 0),
        UnitCost      DECIMAL(18,2) NOT NULL CONSTRAINT CK_PurchaseReceiptLine_UnitCost CHECK (UnitCost >= 0),
        CONSTRAINT FK_PurchaseReceiptLine_Receipt FOREIGN KEY (ReceiptID)  REFERENCES [mn].[PurchaseReceipt](ReceiptID),
        CONSTRAINT FK_PurchaseReceiptLine_Part    FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber)
    );
END;
GO

IF OBJECT_ID(N'[mn].[SalesInvoiceLine]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[SalesInvoiceLine]
    (
        InvoiceLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceID     INT NOT NULL,
        PartNumber    NVARCHAR(100) NOT NULL,
        Quantity      INT NOT NULL CONSTRAINT CK_SalesInvoiceLine_Quantity CHECK (Quantity > 0),
        UnitPrice     DECIMAL(18,2) NOT NULL CONSTRAINT CK_SalesInvoiceLine_UnitPrice CHECK (UnitPrice >= 0),
        UnitCost      DECIMAL(18,2) NOT NULL CONSTRAINT CK_SalesInvoiceLine_UnitCost  CHECK (UnitCost  >= 0),
        CONSTRAINT FK_SalesInvoiceLine_Invoice FOREIGN KEY (InvoiceID)  REFERENCES [mn].[SalesInvoice](InvoiceID),
        CONSTRAINT FK_SalesInvoiceLine_Part    FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber)
    );
END;
GO

IF OBJECT_ID(N'[mn].[Payment]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Payment]
    (
        PaymentID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PaymentDate   DATETIME2 NOT NULL CONSTRAINT DF_Payment_PaymentDate DEFAULT SYSDATETIME(),
        Direction     NVARCHAR(10) NOT NULL,
        PartyType     NVARCHAR(20) NOT NULL,
        PartyID       INT NOT NULL,
        ReferenceType NVARCHAR(30) NOT NULL,
        ReferenceID   INT NULL,
        Amount        DECIMAL(18,2) NOT NULL CONSTRAINT CK_Payment_Amount CHECK (Amount > 0),
        Method        NVARCHAR(30) NOT NULL,
        Notes         NVARCHAR(500) NULL,
        CreatedByUserID INT NULL,
        CONSTRAINT CK_Payment_Direction CHECK (Direction IN (N'IN', N'OUT')),
        CONSTRAINT CK_Payment_PartyType CHECK (PartyType IN (N'CUSTOMER', N'SUPPLIER')),
        CONSTRAINT FK_Payment_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

/* ---------------------------------------------------------------------------
   4. Expense / inventory adjustment / audit tables
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'[mn].[Expense]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Expense]
    (
        ExpenseID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ExpenseDate      DATETIME2 NOT NULL CONSTRAINT DF_Expense_ExpenseDate DEFAULT SYSDATETIME(),
        ExpenseAccountID INT NOT NULL,
        Amount           DECIMAL(18,2) NOT NULL CONSTRAINT CK_Expense_Amount CHECK (Amount > 0),
        Description      NVARCHAR(500) NOT NULL,
        PaymentMethod    NVARCHAR(30) NOT NULL,
        CreatedByUserID  INT NULL,
        CONSTRAINT FK_Expense_Account FOREIGN KEY (ExpenseAccountID) REFERENCES [mn].[Account](AccountID),
        CONSTRAINT FK_Expense_User    FOREIGN KEY (CreatedByUserID)  REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[InventoryAdjustment]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[InventoryAdjustment]
    (
        AdjustmentID     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PartNumber       NVARCHAR(100) NOT NULL,
        PreviousQuantity INT NOT NULL,
        NewQuantity      INT NOT NULL,
        Difference       INT NOT NULL,
        Reason           NVARCHAR(500) NOT NULL,
        CreatedByUserID  INT NULL,
        CreatedAt        DATETIME2 NOT NULL CONSTRAINT DF_InventoryAdjustment_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT FK_InventoryAdjustment_Part FOREIGN KEY (PartNumber)      REFERENCES [mn].[SpareParts](PartNumber),
        CONSTRAINT FK_InventoryAdjustment_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[AuditLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[AuditLog]
    (
        AuditLogID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID     INT NULL,
        ActionCode NVARCHAR(50)  NOT NULL,
        EntityType NVARCHAR(50)  NULL,
        EntityID   INT NULL,
        Details    NVARCHAR(1000) NULL,
        CreatedAt  DATETIME2 NOT NULL CONSTRAINT DF_AuditLog_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT FK_AuditLog_User FOREIGN KEY (UserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

/* ---------------------------------------------------------------------------
   5. Chart of accounts
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'1000')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'1000', N'Cash', N'ASSET');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'1100')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'1100', N'Accounts Receivable', N'ASSET');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'1200')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'1200', N'Inventory', N'ASSET');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'2000')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'2000', N'Accounts Payable', N'LIABILITY');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'4000')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'4000', N'Sales Revenue', N'REVENUE');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'5000')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'5000', N'Cost of Goods Sold', N'EXPENSE');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'6000')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'6000', N'Operating Expenses', N'EXPENSE');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'6100')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'6100', N'Rent', N'EXPENSE');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'6200')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'6200', N'Salaries and Wages', N'EXPENSE');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'6300')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'6300', N'Utilities', N'EXPENSE');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'6400')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'6400', N'Transport', N'EXPENSE');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'6900')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'6900', N'Miscellaneous Expenses', N'EXPENSE');
GO

/* ---------------------------------------------------------------------------
   6. Roles - Clerk and Accountant
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [mn].[Role] WHERE RoleName = N'Clerk')
    INSERT INTO [mn].[Role] (RoleName) VALUES (N'Clerk');
IF NOT EXISTS (SELECT 1 FROM [mn].[Role] WHERE RoleName = N'Accountant')
    INSERT INTO [mn].[Role] (RoleName) VALUES (N'Accountant');

/* Make sure every permission we reference exists (the column is PermissionName
   in this database; some scripts call it PermissionCode). */
DECLARE @permColumn SYSNAME;
SELECT @permColumn = COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
 WHERE TABLE_SCHEMA = 'mn' AND TABLE_NAME = 'Permission'
   AND COLUMN_NAME IN ('PermissionName', 'PermissionCode');
IF @permColumn IS NULL SET @permColumn = N'PermissionName';

/* Clerk: front-counter sales and stock movement. The permission column is
   dynamic (@permColumn), so the statement is built and run as dynamic SQL. */
DECLARE @sqlClerk NVARCHAR(MAX) =
    N'INSERT INTO [mn].[RolePermission] (RoleID, PermissionID)
      SELECT r.RoleID, p.PermissionID
      FROM [mn].[Role] r
      JOIN [mn].[Permission] p ON p.' + QUOTENAME(@permColumn) + N' IN (N''INVENTORY_RECEIVE'', N''INVENTORY_ISSUE'', N''SALES_MANAGE'', N''RETURN_MANAGE'')
      WHERE r.RoleName = N''Clerk''
        AND NOT EXISTS (SELECT 1 FROM [mn].[RolePermission] rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID)';
EXEC sp_executesql @sqlClerk;

/* Accountant: bookkeeping, expenses, payments and reports. */
DECLARE @sqlAccountant NVARCHAR(MAX) =
    N'INSERT INTO [mn].[RolePermission] (RoleID, PermissionID)
      SELECT r.RoleID, p.PermissionID
      FROM [mn].[Role] r
      JOIN [mn].[Permission] p ON p.' + QUOTENAME(@permColumn) + N' IN (N''ACCOUNTING_VIEW'', N''REPORT_VIEW'', N''EXPENSE_MANAGE'', N''PAYMENT_MANAGE'')
      WHERE r.RoleName = N''Accountant''
        AND NOT EXISTS (SELECT 1 FROM [mn].[RolePermission] rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID)';
EXEC sp_executesql @sqlAccountant;
GO

/* ---------------------------------------------------------------------------
   7. Spare-parts catalogue (cars), with matching batches and stock cards
   --------------------------------------------------------------------------- */
DECLARE @Parts TABLE
(
    PartNumber    NVARCHAR(100),
    Description   NVARCHAR(500),
    CategoryName  NVARCHAR(100),
    ConditionName NVARCHAR(100),
    SupplierName  NVARCHAR(200),
    Quantity      INT,
    UnitPrice     DECIMAL(18,2),
    ReorderLevel  INT
);

INSERT INTO @Parts (PartNumber, Description, CategoryName, ConditionName, SupplierName, Quantity, UnitPrice, ReorderLevel) VALUES
 (N'ENG-OIL-FLT-01', N'Oil filter - Toyota Corolla 1.8',        N'Filters',              N'New',         N'Addis Auto Parts',   40,  320.00, 10),
 (N'ENG-AIR-FLT-01', N'Air filter element - Toyota Hilux',      N'Filters',              N'New',         N'Addis Auto Parts',   25,  450.00,  8),
 (N'ENG-FUEL-FLT-01',N'Fuel filter - Toyota Hilux 2.5D',        N'Filters',              N'New',         N'Merkato Spare Parts',30,  520.00,  8),
 (N'ENG-SPK-PLG-01', N'Spark plug Iridium set of 4',            N'Ignition',             N'New',         N'Bole Auto Supply',   60,  780.00, 12),
 (N'ENG-IGN-COIL-01',N'Ignition coil - Toyota Corolla',         N'Ignition',             N'New',         N'Bole Auto Supply',   20, 1650.00,  6),
 (N'ENG-TIM-BLT-01', N'Timing belt kit - Corolla 1.6',          N'Belts & Hoses',        N'New',         N'Merkato Spare Parts',15, 2600.00,  5),
 (N'ENG-SER-BLT-01', N'Serpentine belt - Toyota Hilux 2.5D',    N'Belts & Hoses',        N'New',         N'Merkato Spare Parts',26,  950.00,  8),
 (N'ENG-WTR-PMP-01', N'Water pump - Toyota Corolla 1.6',        N'Cooling',              N'New',         N'Bole Auto Supply',   12, 3200.00,  4),
 (N'COOL-THRMO-01',  N'Thermostat 82C - Toyota Corolla',        N'Cooling',              N'New',         N'Addis Auto Parts',   28,  620.00,  8),
 (N'COOL-RAD-HOS-01',N'Radiator upper hose - Corolla 1.6',      N'Belts & Hoses',        N'New',         N'Addis Auto Parts',   22,  480.00,  8),
 (N'BRK-PAD-FRT-01', N'Front brake pad set - Toyota Hilux',     N'Brakes',               N'New',         N'Addis Auto Parts',   35, 1450.00, 10),
 (N'BRK-PAD-RER-01', N'Rear brake shoe set - Toyota Corolla',   N'Brakes',               N'New',         N'Merkato Spare Parts',20, 1100.00,  8),
 (N'BRK-DSC-FRT-01', N'Front brake disc pair - Toyota Hilux',   N'Brakes',               N'New',         N'Bole Auto Supply',   18, 2900.00,  6),
 (N'BRK-FLD-DOT4-01',N'Brake fluid DOT4 500ml',                 N'Brakes',               N'New',         N'Addis Auto Parts',   50,  260.00, 15),
 (N'SUS-SHK-FRT-01', N'Front shock absorber - Toyota Hilux',    N'Suspension & Steering',N'New',         N'Bole Auto Supply',   16, 3800.00,  6),
 (N'SUS-CTL-ARM-01', N'Lower control arm - Toyota Corolla',     N'Suspension & Steering',N'New',         N'Merkato Spare Parts',10, 4200.00,  4),
 (N'SUS-WHL-BRG-01', N'Front wheel bearing kit - Corolla',      N'Suspension & Steering',N'New',         N'Addis Auto Parts',   24, 1750.00,  8),
 (N'ELC-BAT-12V-01', N'Battery 12V 70Ah',                       N'Electrical',           N'New',         N'Bole Auto Supply',   10, 6800.00,  4),
 (N'ELC-ALT-01',     N'Alternator 90A - Toyota Hilux',          N'Electrical',           N'Refurbished', N'Merkato Spare Parts', 6, 9500.00,  3),
 (N'ELC-STR-01',     N'Starter motor - Toyota Corolla',         N'Electrical',           N'Refurbished', N'Merkato Spare Parts', 7, 8200.00,  3),
 (N'ELC-HLB-H4-01',  N'Headlight bulb H4 12V',                  N'Electrical',           N'New',         N'Addis Auto Parts',   80,  180.00, 20),
 (N'ELC-WPR-BLD-01', N'Wiper blade 24 inch',                    N'Body & Exterior',      N'New',         N'Addis Auto Parts',   45,  350.00, 12),
 (N'TRN-CLT-KIT-01', N'Clutch kit - Toyota Hilux 2.5D',         N'Transmission',         N'New',         N'Bole Auto Supply',    9, 7400.00,  3),
 (N'TRN-CV-JNT-01',  N'CV joint outer - Toyota Corolla',        N'Transmission',         N'New',         N'Merkato Spare Parts',14, 3100.00,  5),
 (N'BDY-SID-MIR-01', N'Side mirror right - Toyota Corolla',     N'Body & Exterior',      N'New',         N'Addis Auto Parts',   13, 2200.00,  4),
 (N'BDY-HEAD-LMP-01',N'Headlight assembly - Toyota Hilux',      N'Body & Exterior',      N'New',         N'Bole Auto Supply',    8, 5600.00,  3);

/* Categories / conditions / suppliers referenced by the catalogue. */
INSERT INTO [mn].[Category] (CategoryName)
SELECT DISTINCT p.CategoryName FROM @Parts p
WHERE NOT EXISTS (SELECT 1 FROM [mn].[Category] c WHERE c.CategoryName = p.CategoryName);

INSERT INTO [mn].[Condition] (ConditionName)
SELECT DISTINCT p.ConditionName FROM @Parts p
WHERE NOT EXISTS (SELECT 1 FROM [mn].[Condition] c WHERE c.ConditionName = p.ConditionName);

INSERT INTO [mn].[Supplier] (SupplierName, Phone, Address)
SELECT DISTINCT p.SupplierName, NULL, NULL FROM @Parts p
WHERE NOT EXISTS (SELECT 1 FROM [mn].[Supplier] s WHERE s.SupplierName = p.SupplierName);

/* Spare parts themselves. */
INSERT INTO [mn].[SpareParts] (PartNumber, Description, CategoryID, ConditionID, SupplierID, Quantity, UnitPrice, ReorderLevel)
SELECT p.PartNumber, p.Description, c.CategoryID, cd.ConditionID, s.SupplierID, p.Quantity, p.UnitPrice, p.ReorderLevel
FROM @Parts p
JOIN [mn].[Category]  c  ON c.CategoryName  = p.CategoryName
JOIN [mn].[Condition] cd ON cd.ConditionName = p.ConditionName
LEFT JOIN [mn].[Supplier] s ON s.SupplierName = p.SupplierName
WHERE NOT EXISTS (SELECT 1 FROM [mn].[SpareParts] sp WHERE sp.PartNumber = p.PartNumber);

/* One opening purchase batch per part so FIFO issuing has stock to draw on.
   Unit cost is set to 70% of the selling price. */
INSERT INTO [mn].[PurchaseBatch] (PartNumber, SupplierID, PurchaseDate, QuantityPurchased, QuantityRemaining, UnitCost)
SELECT p.PartNumber, s.SupplierID, SYSDATETIME(), p.Quantity, p.Quantity, CONVERT(DECIMAL(18,2), p.UnitPrice * 0.70)
FROM @Parts p
LEFT JOIN [mn].[Supplier] s ON s.SupplierName = p.SupplierName
WHERE NOT EXISTS (SELECT 1 FROM [mn].[PurchaseBatch] pb WHERE pb.PartNumber = p.PartNumber);

/* Matching stock-card movements. */
INSERT INTO [mn].[StockCard] (PartNumber, MovementType, Quantity, ReferenceType, Remarks)
SELECT p.PartNumber, N'IN', p.Quantity, N'Opening', N'Opening balance (seed data)'
FROM @Parts p
WHERE NOT EXISTS (SELECT 1 FROM [mn].[StockCard] sc WHERE sc.PartNumber = p.PartNumber);

PRINT 'Repair + seed complete.';
GO
