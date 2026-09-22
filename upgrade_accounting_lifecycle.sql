USE [Inventory];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[mn].[Account]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Account]
    (
        AccountID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AccountCode NVARCHAR(20) NOT NULL UNIQUE,
        AccountName NVARCHAR(150) NOT NULL,
        AccountType NVARCHAR(30) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Account_IsActive DEFAULT 1
    );
END;
GO

IF OBJECT_ID(N'[mn].[JournalEntry]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[JournalEntry]
    (
        JournalEntryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EntryDate DATETIME2 NOT NULL CONSTRAINT DF_JournalEntry_EntryDate DEFAULT SYSDATETIME(),
        ReferenceType NVARCHAR(50) NOT NULL,
        ReferenceID INT NULL,
        Description NVARCHAR(500) NOT NULL,
        CreatedByUserID INT NULL,
        CONSTRAINT FK_JournalEntry_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[JournalLine]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[JournalLine]
    (
        JournalLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        JournalEntryID INT NOT NULL,
        AccountID INT NOT NULL,
        Debit DECIMAL(18,2) NOT NULL CONSTRAINT CK_JournalLine_Debit CHECK (Debit >= 0),
        Credit DECIMAL(18,2) NOT NULL CONSTRAINT CK_JournalLine_Credit CHECK (Credit >= 0),
        CONSTRAINT CK_JournalLine_OneSide CHECK ((Debit > 0 AND Credit = 0) OR (Credit > 0 AND Debit = 0)),
        CONSTRAINT FK_JournalLine_Entry FOREIGN KEY (JournalEntryID) REFERENCES [mn].[JournalEntry](JournalEntryID),
        CONSTRAINT FK_JournalLine_Account FOREIGN KEY (AccountID) REFERENCES [mn].[Account](AccountID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[PurchaseReceipt]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[PurchaseReceipt]
    (
        ReceiptID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierID INT NOT NULL,
        ReceiptDate DATETIME2 NOT NULL CONSTRAINT DF_PurchaseReceipt_ReceiptDate DEFAULT SYSDATETIME(),
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT CK_PurchaseReceipt_Total CHECK (TotalAmount >= 0),
        AmountPaid DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseReceipt_AmountPaid DEFAULT 0,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_PurchaseReceipt_Status DEFAULT 'UNPAID',
        Notes NVARCHAR(500) NULL,
        CreatedByUserID INT NULL,
        CONSTRAINT FK_PurchaseReceipt_Supplier FOREIGN KEY (SupplierID) REFERENCES [mn].[Supplier](SupplierID),
        CONSTRAINT FK_PurchaseReceipt_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[PurchaseReceiptLine]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[PurchaseReceiptLine]
    (
        ReceiptLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReceiptID INT NOT NULL,
        PartNumber NVARCHAR(100) NOT NULL,
        Quantity INT NOT NULL CONSTRAINT CK_PurchaseReceiptLine_Quantity CHECK (Quantity > 0),
        UnitCost DECIMAL(18,2) NOT NULL CONSTRAINT CK_PurchaseReceiptLine_UnitCost CHECK (UnitCost >= 0),
        LineTotal AS (CONVERT(DECIMAL(18,2), Quantity * UnitCost)) PERSISTED,
        CONSTRAINT FK_PurchaseReceiptLine_Receipt FOREIGN KEY (ReceiptID) REFERENCES [mn].[PurchaseReceipt](ReceiptID),
        CONSTRAINT FK_PurchaseReceiptLine_Part FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber)
    );
END;
GO

IF OBJECT_ID(N'[mn].[SalesInvoice]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[SalesInvoice]
    (
        InvoiceID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CustomerID INT NOT NULL,
        InvoiceDate DATETIME2 NOT NULL CONSTRAINT DF_SalesInvoice_InvoiceDate DEFAULT SYSDATETIME(),
        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT CK_SalesInvoice_Subtotal CHECK (Subtotal >= 0),
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_TaxAmount DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT CK_SalesInvoice_Total CHECK (TotalAmount >= 0),
        AmountPaid DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_AmountPaid DEFAULT 0,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_SalesInvoice_Status DEFAULT 'UNPAID',
        Remarks NVARCHAR(500) NULL,
        CreatedByUserID INT NULL,
        CONSTRAINT FK_SalesInvoice_Customer FOREIGN KEY (CustomerID) REFERENCES [mn].[Customer](CustomerID),
        CONSTRAINT FK_SalesInvoice_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[SalesInvoiceLine]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[SalesInvoiceLine]
    (
        InvoiceLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceID INT NOT NULL,
        PartNumber NVARCHAR(100) NOT NULL,
        Quantity INT NOT NULL CONSTRAINT CK_SalesInvoiceLine_Quantity CHECK (Quantity > 0),
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_SalesInvoiceLine_UnitPrice CHECK (UnitPrice >= 0),
        UnitCost DECIMAL(18,2) NOT NULL CONSTRAINT CK_SalesInvoiceLine_UnitCost CHECK (UnitCost >= 0),
        LineTotal AS (CONVERT(DECIMAL(18,2), Quantity * UnitPrice)) PERSISTED,
        COGS AS (CONVERT(DECIMAL(18,2), Quantity * UnitCost)) PERSISTED,
        CONSTRAINT FK_SalesInvoiceLine_Invoice FOREIGN KEY (InvoiceID) REFERENCES [mn].[SalesInvoice](InvoiceID),
        CONSTRAINT FK_SalesInvoiceLine_Part FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber)
    );
END;
GO

IF OBJECT_ID(N'[mn].[Payment]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Payment]
    (
        PaymentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PaymentDate DATETIME2 NOT NULL CONSTRAINT DF_Payment_PaymentDate DEFAULT SYSDATETIME(),
        Direction NVARCHAR(10) NOT NULL,
        PartyType NVARCHAR(20) NOT NULL,
        PartyID INT NOT NULL,
        ReferenceType NVARCHAR(30) NOT NULL,
        ReferenceID INT NULL,
        Amount DECIMAL(18,2) NOT NULL CONSTRAINT CK_Payment_Amount CHECK (Amount > 0),
        Method NVARCHAR(30) NOT NULL,
        Notes NVARCHAR(500) NULL,
        CreatedByUserID INT NULL,
        CONSTRAINT CK_Payment_Direction CHECK (Direction IN ('IN', 'OUT')),
        CONSTRAINT CK_Payment_PartyType CHECK (PartyType IN ('CUSTOMER', 'SUPPLIER')),
        CONSTRAINT FK_Payment_User FOREIGN KEY (CreatedByUserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

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
GO
