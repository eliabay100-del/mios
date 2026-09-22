USE [Inventory];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'mn.SpareParts', N'ReorderLevel') IS NULL
    ALTER TABLE [mn].[SpareParts] ADD ReorderLevel INT NOT NULL CONSTRAINT DF_SpareParts_ReorderLevel DEFAULT 5;
IF COL_LENGTH(N'mn.SpareParts', N'MaxStockLevel') IS NULL
    ALTER TABLE [mn].[SpareParts] ADD MaxStockLevel INT NULL;
GO

IF OBJECT_ID(N'[mn].[InventoryAdjustment]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[InventoryAdjustment]
    (
        AdjustmentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PartNumber NVARCHAR(100) NOT NULL,
        PreviousQuantity INT NOT NULL,
        NewQuantity INT NOT NULL,
        Difference INT NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        CreatedByUserID INT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_InventoryAdjustment_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT FK_InventoryAdjustment_Part FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber),
        CONSTRAINT FK_InventoryAdjustment_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[SalesReturn]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[SalesReturn]
    (
        SalesReturnID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceID INT NULL,
        CustomerID INT NOT NULL,
        PartNumber NVARCHAR(100) NOT NULL,
        Quantity INT NOT NULL CONSTRAINT CK_SalesReturn_Quantity CHECK (Quantity > 0),
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_SalesReturn_UnitPrice CHECK (UnitPrice >= 0),
        Reason NVARCHAR(500) NOT NULL,
        ReturnDate DATETIME2 NOT NULL CONSTRAINT DF_SalesReturn_ReturnDate DEFAULT SYSDATETIME(),
        CreatedByUserID INT NULL,
        CONSTRAINT FK_SalesReturn_Invoice FOREIGN KEY (InvoiceID) REFERENCES [mn].[SalesInvoice](InvoiceID),
        CONSTRAINT FK_SalesReturn_Customer FOREIGN KEY (CustomerID) REFERENCES [mn].[Customer](CustomerID),
        CONSTRAINT FK_SalesReturn_Part FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber),
        CONSTRAINT FK_SalesReturn_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[PurchaseReturn]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[PurchaseReturn]
    (
        PurchaseReturnID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReceiptID INT NULL,
        SupplierID INT NOT NULL,
        PartNumber NVARCHAR(100) NOT NULL,
        Quantity INT NOT NULL CONSTRAINT CK_PurchaseReturn_Quantity CHECK (Quantity > 0),
        UnitCost DECIMAL(18,2) NOT NULL CONSTRAINT CK_PurchaseReturn_UnitCost CHECK (UnitCost >= 0),
        Reason NVARCHAR(500) NOT NULL,
        ReturnDate DATETIME2 NOT NULL CONSTRAINT DF_PurchaseReturn_ReturnDate DEFAULT SYSDATETIME(),
        CreatedByUserID INT NULL,
        CONSTRAINT FK_PurchaseReturn_Receipt FOREIGN KEY (ReceiptID) REFERENCES [mn].[PurchaseReceipt](ReceiptID),
        CONSTRAINT FK_PurchaseReturn_Supplier FOREIGN KEY (SupplierID) REFERENCES [mn].[Supplier](SupplierID),
        CONSTRAINT FK_PurchaseReturn_Part FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber),
        CONSTRAINT FK_PurchaseReturn_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[Expense]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Expense]
    (
        ExpenseID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ExpenseDate DATETIME2 NOT NULL CONSTRAINT DF_Expense_ExpenseDate DEFAULT SYSDATETIME(),
        ExpenseAccountID INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL CONSTRAINT CK_Expense_Amount CHECK (Amount > 0),
        Description NVARCHAR(500) NOT NULL,
        PaymentMethod NVARCHAR(30) NOT NULL,
        CreatedByUserID INT NULL,
        CONSTRAINT FK_Expense_Account FOREIGN KEY (ExpenseAccountID) REFERENCES [mn].[Account](AccountID),
        CONSTRAINT FK_Expense_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'REPORT_VIEW')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'REPORT_VIEW', N'View financial reports');
IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'RETURN_MANAGE')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'RETURN_MANAGE', N'Process purchase and sales returns');
IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'EXPENSE_MANAGE')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'EXPENSE_MANAGE', N'Record expenses');
IF NOT EXISTS (SELECT 1 FROM [mn].[Account] WHERE AccountCode = N'6000')
    INSERT INTO [mn].[Account] (AccountCode, AccountName, AccountType) VALUES (N'6000', N'Operating Expenses', N'EXPENSE');

INSERT INTO [mn].[RolePermission] (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM [mn].[Role] r CROSS JOIN [mn].[Permission] p
WHERE r.RoleName = N'Admin'
  AND NOT EXISTS (SELECT 1 FROM [mn].[RolePermission] rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);

INSERT INTO [mn].[RolePermission] (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM [mn].[Role] r JOIN [mn].[Permission] p ON p.PermissionCode IN (N'REPORT_VIEW', N'RETURN_MANAGE')
WHERE r.RoleName = N'Manager'
  AND NOT EXISTS (SELECT 1 FROM [mn].[RolePermission] rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);

INSERT INTO [mn].[RolePermission] (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM [mn].[Role] r JOIN [mn].[Permission] p ON p.PermissionCode IN (N'REPORT_VIEW', N'EXPENSE_MANAGE')
WHERE r.RoleName = N'Accountant'
  AND NOT EXISTS (SELECT 1 FROM [mn].[RolePermission] rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);
GO
