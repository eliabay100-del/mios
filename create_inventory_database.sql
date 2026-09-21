IF DB_ID(N'Inventory') IS NULL
    CREATE DATABASE [Inventory];
GO

USE [Inventory];
GO

IF SCHEMA_ID(N'mn') IS NULL
    EXEC(N'CREATE SCHEMA [mn]');
GO

IF OBJECT_ID(N'[mn].[Category]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Category]
    (
        CategoryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CategoryName NVARCHAR(100) NOT NULL UNIQUE
    );
END;
GO

IF OBJECT_ID(N'[mn].[Condition]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Condition]
    (
        ConditionID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConditionName NVARCHAR(100) NOT NULL UNIQUE
    );
END;
GO

IF OBJECT_ID(N'[mn].[Supplier]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Supplier]
    (
        SupplierID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierName NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(50) NULL,
        Address NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Supplier_CreatedAt DEFAULT SYSDATETIME()
    );
END;
GO

IF OBJECT_ID(N'[mn].[Customer]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Customer]
    (
        CustomerID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CustomerName NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(50) NULL,
        Email NVARCHAR(200) NULL,
        Address NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Customer_CreatedAt DEFAULT SYSDATETIME()
    );
END;
GO

IF OBJECT_ID(N'[mn].[UUser]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[UUser]
    (
        UserID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(500) NOT NULL
    );
END;
GO

IF OBJECT_ID(N'[mn].[SpareParts]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[SpareParts]
    (
        PartID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PartNumber NVARCHAR(100) NOT NULL UNIQUE,
        Description NVARCHAR(500) NOT NULL,
        CategoryID INT NOT NULL,
        ConditionID INT NOT NULL,
        SupplierID INT NULL,
        Quantity INT NOT NULL CONSTRAINT CK_SpareParts_Quantity CHECK (Quantity >= 0),
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_SpareParts_UnitPrice CHECK (UnitPrice >= 0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SpareParts_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT FK_SpareParts_Category FOREIGN KEY (CategoryID) REFERENCES [mn].[Category](CategoryID),
        CONSTRAINT FK_SpareParts_Condition FOREIGN KEY (ConditionID) REFERENCES [mn].[Condition](ConditionID),
        CONSTRAINT FK_SpareParts_Supplier FOREIGN KEY (SupplierID) REFERENCES [mn].[Supplier](SupplierID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[PurchaseBatch]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[PurchaseBatch]
    (
        BatchID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PartNumber NVARCHAR(100) NOT NULL,
        SupplierID INT NULL,
        PurchaseDate DATETIME2 NOT NULL CONSTRAINT DF_PurchaseBatch_PurchaseDate DEFAULT SYSDATETIME(),
        QuantityPurchased INT NOT NULL CONSTRAINT CK_PurchaseBatch_Purchased CHECK (QuantityPurchased > 0),
        QuantityRemaining INT NOT NULL CONSTRAINT CK_PurchaseBatch_Remaining CHECK (QuantityRemaining >= 0),
        UnitCost DECIMAL(18,2) NOT NULL CONSTRAINT CK_PurchaseBatch_UnitCost CHECK (UnitCost >= 0),
        CONSTRAINT FK_PurchaseBatch_Part FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber),
        CONSTRAINT FK_PurchaseBatch_Supplier FOREIGN KEY (SupplierID) REFERENCES [mn].[Supplier](SupplierID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[IssuedItem]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[IssuedItem]
    (
        IssuedItemID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PartNumber NVARCHAR(100) NOT NULL,
        CustomerID INT NOT NULL,
        QuantityIssued INT NOT NULL CONSTRAINT CK_IssuedItem_Quantity CHECK (QuantityIssued > 0),
        IssueDate DATETIME2 NOT NULL CONSTRAINT DF_IssuedItem_IssueDate DEFAULT SYSDATETIME(),
        CONSTRAINT FK_IssuedItem_Part FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber),
        CONSTRAINT FK_IssuedItem_Customer FOREIGN KEY (CustomerID) REFERENCES [mn].[Customer](CustomerID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[StockCard]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[StockCard]
    (
        StockCardID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PartNumber NVARCHAR(100) NOT NULL,
        MovementType NVARCHAR(3) NOT NULL CONSTRAINT CK_StockCard_MovementType CHECK (MovementType IN ('IN', 'OUT')),
        Quantity INT NOT NULL CONSTRAINT CK_StockCard_Quantity CHECK (Quantity > 0),
        MovementDate DATETIME2 NOT NULL CONSTRAINT DF_StockCard_MovementDate DEFAULT SYSDATETIME(),
        ReferenceType NVARCHAR(100) NULL,
        Remarks NVARCHAR(500) NULL,
        CONSTRAINT FK_StockCard_Part FOREIGN KEY (PartNumber) REFERENCES [mn].[SpareParts](PartNumber)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM [mn].[Category])
    INSERT INTO [mn].[Category] (CategoryName) VALUES (N'General');

IF NOT EXISTS (SELECT 1 FROM [mn].[Condition])
    INSERT INTO [mn].[Condition] (ConditionName) VALUES (N'New');
GO