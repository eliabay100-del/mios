USE [Inventory];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Keep lookup data required by the Add Spare Part form.
DELETE FROM [mn].[JournalLine];
DELETE FROM [mn].[JournalEntry];
DELETE FROM [mn].[Payment];
DELETE FROM [mn].[AuditLog];
DELETE FROM [mn].[RolePermission];
DELETE FROM [mn].[UserRole];
DELETE FROM [mn].[Expense];
DELETE FROM [mn].[PurchaseReturn];
DELETE FROM [mn].[SalesReturn];
DELETE FROM [mn].[InventoryAdjustment];
DELETE FROM [mn].[SalesInvoiceLine];
DELETE FROM [mn].[SalesInvoice];
DELETE FROM [mn].[PurchaseReceiptLine];
DELETE FROM [mn].[PurchaseReceipt];
DELETE FROM [mn].[StockCard];
DELETE FROM [mn].[IssuedItem];
DELETE FROM [mn].[PurchaseBatch];
DELETE FROM [mn].[SpareParts];
DELETE FROM [mn].[Customer];
DELETE FROM [mn].[Supplier];
DELETE FROM [mn].[UUser];

-- Clear the legacy user table too, if it still exists in this database.
IF OBJECT_ID(N'[mn].[Users]', N'U') IS NOT NULL
    DELETE FROM [mn].[Users];

-- Make the empty database behave like a fresh install.
DBCC CHECKIDENT (N'[mn].[StockCard]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[IssuedItem]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[PurchaseBatch]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[SpareParts]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[Customer]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[Supplier]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[UUser]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[JournalLine]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[JournalEntry]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[Payment]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[AuditLog]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[SalesInvoiceLine]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[SalesInvoice]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[PurchaseReceiptLine]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[PurchaseReceipt]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[Expense]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[PurchaseReturn]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[SalesReturn]', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT (N'[mn].[InventoryAdjustment]', RESEED, 0) WITH NO_INFOMSGS;

IF OBJECT_ID(N'[mn].[Users]', N'U') IS NOT NULL
    DBCC CHECKIDENT (N'[mn].[Users]', RESEED, 0) WITH NO_INFOMSGS;

COMMIT TRANSACTION;
GO

SELECT 'UUser' AS TableName, COUNT(*) AS RemainingRows FROM [mn].[UUser]
UNION ALL SELECT 'Customer', COUNT(*) FROM [mn].[Customer]
UNION ALL SELECT 'Supplier', COUNT(*) FROM [mn].[Supplier]
UNION ALL SELECT 'SpareParts', COUNT(*) FROM [mn].[SpareParts]
UNION ALL SELECT 'PurchaseBatch', COUNT(*) FROM [mn].[PurchaseBatch]
UNION ALL SELECT 'IssuedItem', COUNT(*) FROM [mn].[IssuedItem]
UNION ALL SELECT 'StockCard', COUNT(*) FROM [mn].[StockCard]
UNION ALL SELECT 'PurchaseReceipt', COUNT(*) FROM [mn].[PurchaseReceipt]
UNION ALL SELECT 'SalesInvoice', COUNT(*) FROM [mn].[SalesInvoice]
UNION ALL SELECT 'Payment', COUNT(*) FROM [mn].[Payment]
UNION ALL SELECT 'JournalEntry', COUNT(*) FROM [mn].[JournalEntry]
UNION ALL SELECT 'JournalLine', COUNT(*) FROM [mn].[JournalLine];
GO