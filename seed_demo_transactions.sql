/* ============================================================================
   MIOS - Demo / test transactions (add stock, sell, expense, payment)
   ----------------------------------------------------------------------------
   Each batch mirrors exactly what the corresponding form does, so running this
   proves the repaired schema supports the real application flows and leaves a
   little sample history to look at.

   Batches:
     1. Add stock   - receive 10 more of an existing part
     2. Add part    - receive a brand-new part
     3. Sale        - sell 5 units of a part to a new customer (FIFO)
     4. Sale        - sell 2 units of another part to a customer (FIFO)
     5. Expense     - record a rent expense
     6. Payment     - pay off the first sales invoice

   These are guarded with NOT EXISTS on a marker so the script can be re-run
   without duplicating the demo history.
   ============================================================================ */
USE [Inventory];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------------------------------------------------------
   Batch 1 - Add stock for an existing part (mirrors Add_SparePart)
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM mn.AuditLog WHERE Details = N'demo:add-ENG-OIL-FLT-01')
BEGIN
    DECLARE @partNumber NVARCHAR(100) = N'ENG-OIL-FLT-01';
    DECLARE @qty INT = 10;
    DECLARE @price DECIMAL(18,2) = 320.00;
    DECLARE @supplierID INT = (SELECT SupplierID FROM mn.Supplier WHERE SupplierName = N'Addis Auto Parts');
    DECLARE @receiptID INT;

    UPDATE mn.SpareParts SET Quantity = Quantity + @qty, UnitPrice = @price WHERE PartNumber = @partNumber;

    INSERT INTO mn.PurchaseReceipt (SupplierID, TotalAmount, AmountPaid, Status, Notes)
    VALUES (@supplierID, @qty * @price, 0, N'UNPAID', N'Received through Add Spare Part');
    SET @receiptID = SCOPE_IDENTITY();

    INSERT INTO mn.PurchaseReceiptLine (ReceiptID, PartNumber, Quantity, UnitCost)
    VALUES (@receiptID, @partNumber, @qty, @price);

    INSERT INTO mn.PurchaseBatch (PartNumber, SupplierID, PurchaseDate, QuantityPurchased, QuantityRemaining, UnitCost)
    VALUES (@partNumber, @supplierID, SYSDATETIME(), @qty, @qty, @price);

    INSERT INTO mn.StockCard (PartNumber, MovementType, Quantity, ReferenceType, Remarks)
    VALUES (@partNumber, N'IN', @qty, N'Purchase', N'Added from spare part form');

    DECLARE @entryID INT;
    INSERT INTO mn.JournalEntry (ReferenceType, ReferenceID, Description) VALUES (N'PURCHASE', @receiptID, N'Inventory received from supplier');
    SET @entryID = SCOPE_IDENTITY();
    INSERT INTO mn.JournalLine (JournalEntryID, AccountID, Debit, Credit)
    SELECT @entryID, a.AccountID, l.Debit, l.Credit
    FROM (VALUES (N'1200', CAST(@qty * @price AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2))),
                 (N'2000', CAST(0 AS DECIMAL(18,2)), CAST(@qty * @price AS DECIMAL(18,2)))) AS l(Code, Debit, Credit)
    JOIN mn.Account a ON a.AccountCode = l.Code;

    INSERT INTO mn.AuditLog (UserID, ActionCode, EntityType, EntityID, Details)
    VALUES (8, N'JOURNAL_POSTED', N'JournalEntry', @entryID, N'demo:add-ENG-OIL-FLT-01');

    PRINT 'Batch 1: received 10 x ENG-OIL-FLT-01 (receipt ' + CAST(@receiptID AS NVARCHAR(10)) + ')';
END;
GO

/* ---------------------------------------------------------------------------
   Batch 2 - Add a brand-new part (mirrors Add_SparePart, new part branch)
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM mn.AuditLog WHERE Details = N'demo:add-ENG-EXH-MFL-01')
BEGIN
    DECLARE @partNumber NVARCHAR(100) = N'ENG-EXH-MFL-01';
    DECLARE @desc NVARCHAR(500) = N'Muffler / silencer - Toyota Corolla 1.6';
    DECLARE @qty INT = 8;
    DECLARE @price DECIMAL(18,2) = 2400.00;
    DECLARE @categoryID INT = (SELECT CategoryID FROM mn.Category WHERE CategoryName = N'Body & Exterior');
    DECLARE @conditionID INT = (SELECT ConditionID FROM mn.Condition WHERE ConditionName = N'New');
    DECLARE @supplierID INT = (SELECT SupplierID FROM mn.Supplier WHERE SupplierName = N'Merkato Spare Parts');
    DECLARE @receiptID INT;

    INSERT INTO mn.SpareParts (PartNumber, Description, CategoryID, ConditionID, SupplierID, Quantity, UnitPrice, ReorderLevel)
    VALUES (@partNumber, @desc, @categoryID, @conditionID, @supplierID, @qty, @price, 4);

    INSERT INTO mn.PurchaseReceipt (SupplierID, TotalAmount, AmountPaid, Status, Notes)
    VALUES (@supplierID, @qty * @price, 0, N'UNPAID', N'Received through Add Spare Part');
    SET @receiptID = SCOPE_IDENTITY();

    INSERT INTO mn.PurchaseReceiptLine (ReceiptID, PartNumber, Quantity, UnitCost)
    VALUES (@receiptID, @partNumber, @qty, @price);

    INSERT INTO mn.PurchaseBatch (PartNumber, SupplierID, PurchaseDate, QuantityPurchased, QuantityRemaining, UnitCost)
    VALUES (@partNumber, @supplierID, SYSDATETIME(), @qty, @qty, CONVERT(DECIMAL(18,2), @price * 0.70));

    INSERT INTO mn.StockCard (PartNumber, MovementType, Quantity, ReferenceType, Remarks)
    VALUES (@partNumber, N'IN', @qty, N'Purchase', N'Added from spare part form');

    DECLARE @entryID INT;
    INSERT INTO mn.JournalEntry (ReferenceType, ReferenceID, Description) VALUES (N'PURCHASE', @receiptID, N'Inventory received from supplier');
    SET @entryID = SCOPE_IDENTITY();
    INSERT INTO mn.JournalLine (JournalEntryID, AccountID, Debit, Credit)
    SELECT @entryID, a.AccountID, l.Debit, l.Credit
    FROM (VALUES (N'1200', CAST(@qty * @price AS DECIMAL(18,2)), CAST(0 AS DECIMAL(18,2))),
                 (N'2000', CAST(0 AS DECIMAL(18,2)), CAST(@qty * @price AS DECIMAL(18,2)))) AS l(Code, Debit, Credit)
    JOIN mn.Account a ON a.AccountCode = l.Code;

    INSERT INTO mn.AuditLog (UserID, ActionCode, EntityType, EntityID, Details)
    VALUES (8, N'JOURNAL_POSTED', N'JournalEntry', @entryID, N'demo:add-ENG-EXH-MFL-01');

    PRINT 'Batch 2: created + received 8 x ENG-EXH-MFL-01 (receipt ' + CAST(@receiptID AS NVARCHAR(10)) + ')';
END;
GO

/* ---------------------------------------------------------------------------
   Batch 3 - Sale (mirrors Issue_SparePart): new customer, FIFO issue
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM mn.AuditLog WHERE Details = N'demo:sale-ENG-OIL-FLT-01')
BEGIN
    DECLARE @partNumber NVARCHAR(100) = N'ENG-OIL-FLT-01';
    DECLARE @qty INT = 5;
    DECLARE @sellPrice DECIMAL(18,2) = 420.00;
    DECLARE @customerID INT;
    DECLARE @invoiceID INT;

    INSERT INTO mn.Customer (CustomerName, Phone, Address) VALUES (N'Abebe Kebede', N'0911000001', N'Addis Ababa');
    SET @customerID = SCOPE_IDENTITY();

    INSERT INTO mn.SalesInvoice (CustomerID, Subtotal, TaxAmount, TotalAmount, AmountPaid, Status, Remarks)
    VALUES (@customerID, 0, 0, 0, 0, N'UNPAID', N'Walk-in sale');
    SET @invoiceID = SCOPE_IDENTITY();

    DECLARE @remaining INT = @qty;
    DECLARE @batchID INT, @batchRemaining INT, @unitCost DECIMAL(18,2), @use INT;
    DECLARE @cogs DECIMAL(18,2) = 0;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT BatchID, QuantityRemaining, UnitCost FROM mn.PurchaseBatch
        WHERE PartNumber = @partNumber AND QuantityRemaining > 0
        ORDER BY PurchaseDate, BatchID;
    OPEN cur;
    FETCH NEXT FROM cur INTO @batchID, @batchRemaining, @unitCost;
    WHILE @@FETCH_STATUS = 0 AND @remaining > 0
    BEGIN
        SET @use = CASE WHEN @remaining < @batchRemaining THEN @remaining ELSE @batchRemaining END;
        UPDATE mn.PurchaseBatch SET QuantityRemaining = QuantityRemaining - @use WHERE BatchID = @batchID;
        INSERT INTO mn.IssuedItem (PartNumber, CustomerID, QuantityIssued) VALUES (@partNumber, @customerID, @use);
        SET @cogs = @cogs + @use * @unitCost;
        SET @remaining = @remaining - @use;
        FETCH NEXT FROM cur INTO @batchID, @batchRemaining, @unitCost;
    END
    CLOSE cur; DEALLOCATE cur;

    IF @remaining > 0 RAISERROR('Not enough stock while issuing %s', 16, 1, @partNumber);

    INSERT INTO mn.SalesInvoiceLine (InvoiceID, PartNumber, Quantity, UnitPrice, UnitCost)
    VALUES (@invoiceID, @partNumber, @qty, @sellPrice, @cogs / @qty);
    UPDATE mn.SpareParts SET Quantity = Quantity - @qty WHERE PartNumber = @partNumber;
    INSERT INTO mn.StockCard (PartNumber, MovementType, Quantity, ReferenceType, Remarks)
    VALUES (@partNumber, N'OUT', @qty, N'Issue', N'Walk-in sale');
    UPDATE mn.SalesInvoice SET Subtotal = @qty * @sellPrice, TotalAmount = @qty * @sellPrice WHERE InvoiceID = @invoiceID;

    DECLARE @revenue DECIMAL(18,2) = @qty * @sellPrice;
    DECLARE @entryID INT;
    INSERT INTO mn.JournalEntry (ReferenceType, ReferenceID, Description) VALUES (N'SALE', @invoiceID, N'Spare parts issued to customer');
    SET @entryID = SCOPE_IDENTITY();
    INSERT INTO mn.JournalLine (JournalEntryID, AccountID, Debit, Credit)
    SELECT @entryID, a.AccountID, l.Debit, l.Credit
    FROM (VALUES (N'1100', @revenue, CAST(0 AS DECIMAL(18,2))),
                 (N'4000', CAST(0 AS DECIMAL(18,2)), @revenue),
                 (N'5000', @cogs,    CAST(0 AS DECIMAL(18,2))),
                 (N'1200', CAST(0 AS DECIMAL(18,2)), @cogs)) AS l(Code, Debit, Credit)
    JOIN mn.Account a ON a.AccountCode = l.Code;

    INSERT INTO mn.AuditLog (UserID, ActionCode, EntityType, EntityID, Details)
    VALUES (8, N'JOURNAL_POSTED', N'JournalEntry', @entryID, N'demo:sale-ENG-OIL-FLT-01');

    PRINT 'Batch 3: sold 5 x ENG-OIL-FLT-01 (invoice ' + CAST(@invoiceID AS NVARCHAR(10)) + ', revenue ' + CAST(@revenue AS NVARCHAR(20)) + ', COGS ' + CAST(@cogs AS NVARCHAR(20)) + ')';
END;
GO

/* ---------------------------------------------------------------------------
   Batch 4 - Second sale, another part
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM mn.AuditLog WHERE Details = N'demo:sale-BRK-PAD-FRT-01')
BEGIN
    DECLARE @partNumber NVARCHAR(100) = N'BRK-PAD-FRT-01';
    DECLARE @qty INT = 2;
    DECLARE @sellPrice DECIMAL(18,2) = 1750.00;
    DECLARE @customerID INT;
    DECLARE @invoiceID INT;

    INSERT INTO mn.Customer (CustomerName, Phone, Address) VALUES (N'Sara Tesfaye', N'0911000002', N'Adama');
    SET @customerID = SCOPE_IDENTITY();

    INSERT INTO mn.SalesInvoice (CustomerID, Subtotal, TaxAmount, TotalAmount, AmountPaid, Status, Remarks)
    VALUES (@customerID, 0, 0, 0, 0, N'UNPAID', N'Walk-in sale');
    SET @invoiceID = SCOPE_IDENTITY();

    DECLARE @remaining INT = @qty;
    DECLARE @batchID INT, @batchRemaining INT, @unitCost DECIMAL(18,2), @use INT;
    DECLARE @cogs DECIMAL(18,2) = 0;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT BatchID, QuantityRemaining, UnitCost FROM mn.PurchaseBatch
        WHERE PartNumber = @partNumber AND QuantityRemaining > 0
        ORDER BY PurchaseDate, BatchID;
    OPEN cur;
    FETCH NEXT FROM cur INTO @batchID, @batchRemaining, @unitCost;
    WHILE @@FETCH_STATUS = 0 AND @remaining > 0
    BEGIN
        SET @use = CASE WHEN @remaining < @batchRemaining THEN @remaining ELSE @batchRemaining END;
        UPDATE mn.PurchaseBatch SET QuantityRemaining = QuantityRemaining - @use WHERE BatchID = @batchID;
        INSERT INTO mn.IssuedItem (PartNumber, CustomerID, QuantityIssued) VALUES (@partNumber, @customerID, @use);
        SET @cogs = @cogs + @use * @unitCost;
        SET @remaining = @remaining - @use;
        FETCH NEXT FROM cur INTO @batchID, @batchRemaining, @unitCost;
    END
    CLOSE cur; DEALLOCATE cur;

    IF @remaining > 0 RAISERROR('Not enough stock while issuing %s', 16, 1, @partNumber);

    INSERT INTO mn.SalesInvoiceLine (InvoiceID, PartNumber, Quantity, UnitPrice, UnitCost)
    VALUES (@invoiceID, @partNumber, @qty, @sellPrice, @cogs / @qty);
    UPDATE mn.SpareParts SET Quantity = Quantity - @qty WHERE PartNumber = @partNumber;
    INSERT INTO mn.StockCard (PartNumber, MovementType, Quantity, ReferenceType, Remarks)
    VALUES (@partNumber, N'OUT', @qty, N'Issue', N'Walk-in sale');
    UPDATE mn.SalesInvoice SET Subtotal = @qty * @sellPrice, TotalAmount = @qty * @sellPrice WHERE InvoiceID = @invoiceID;

    DECLARE @revenue DECIMAL(18,2) = @qty * @sellPrice;
    DECLARE @entryID INT;
    INSERT INTO mn.JournalEntry (ReferenceType, ReferenceID, Description) VALUES (N'SALE', @invoiceID, N'Spare parts issued to customer');
    SET @entryID = SCOPE_IDENTITY();
    INSERT INTO mn.JournalLine (JournalEntryID, AccountID, Debit, Credit)
    SELECT @entryID, a.AccountID, l.Debit, l.Credit
    FROM (VALUES (N'1100', @revenue, CAST(0 AS DECIMAL(18,2))),
                 (N'4000', CAST(0 AS DECIMAL(18,2)), @revenue),
                 (N'5000', @cogs,    CAST(0 AS DECIMAL(18,2))),
                 (N'1200', CAST(0 AS DECIMAL(18,2)), @cogs)) AS l(Code, Debit, Credit)
    JOIN mn.Account a ON a.AccountCode = l.Code;

    INSERT INTO mn.AuditLog (UserID, ActionCode, EntityType, EntityID, Details)
    VALUES (8, N'JOURNAL_POSTED', N'JournalEntry', @entryID, N'demo:sale-BRK-PAD-FRT-01');

    PRINT 'Batch 4: sold 2 x BRK-PAD-FRT-01 (invoice ' + CAST(@invoiceID AS NVARCHAR(10)) + ', revenue ' + CAST(@revenue AS NVARCHAR(20)) + ', COGS ' + CAST(@cogs AS NVARCHAR(20)) + ')';
END;
GO

/* ---------------------------------------------------------------------------
   Batch 5 - Record an expense (mirrors ExpenseForm)
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM mn.AuditLog WHERE Details = N'demo:expense-rent')
BEGIN
    DECLARE @accountID INT = (SELECT AccountID FROM mn.Account WHERE AccountCode = N'6100');
    DECLARE @amount DECIMAL(18,2) = 15000.00;
    DECLARE @expenseID INT;

    INSERT INTO mn.Expense (ExpenseAccountID, Amount, Description, PaymentMethod, CreatedByUserID)
    VALUES (@accountID, @amount, N'Workshop rent - current month', N'Bank transfer', 8);
    SET @expenseID = SCOPE_IDENTITY();

    DECLARE @entryID INT;
    INSERT INTO mn.JournalEntry (ReferenceType, ReferenceID, Description) VALUES (N'EXPENSE', @expenseID, N'Workshop rent - current month');
    SET @entryID = SCOPE_IDENTITY();
    INSERT INTO mn.JournalLine (JournalEntryID, AccountID, Debit, Credit)
    SELECT @entryID, a.AccountID, l.Debit, l.Credit
    FROM (VALUES (N'6100', @amount, CAST(0 AS DECIMAL(18,2))),
                 (N'1000', CAST(0 AS DECIMAL(18,2)), @amount)) AS l(Code, Debit, Credit)
    JOIN mn.Account a ON a.AccountCode = l.Code;

    INSERT INTO mn.AuditLog (UserID, ActionCode, EntityType, EntityID, Details)
    VALUES (8, N'JOURNAL_POSTED', N'JournalEntry', @entryID, N'demo:expense-rent');

    PRINT 'Batch 5: recorded rent expense ' + CAST(@amount AS NVARCHAR(20)) + ' (expense ' + CAST(@expenseID AS NVARCHAR(10)) + ')';
END;
GO

/* ---------------------------------------------------------------------------
   Batch 6 - Record a payment on the first sales invoice (mirrors PaymentForm)
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM mn.AuditLog WHERE Details = N'demo:payment-invoice')
BEGIN
    DECLARE @invoiceID INT = (SELECT MIN(InvoiceID) FROM mn.SalesInvoice);
    DECLARE @customerID INT, @totalAmount DECIMAL(18,2), @amountPaid DECIMAL(18,2);

    SELECT @customerID = CustomerID, @totalAmount = TotalAmount, @amountPaid = AmountPaid
    FROM mn.SalesInvoice WHERE InvoiceID = @invoiceID;

    IF @totalAmount - @amountPaid > 0
    BEGIN
        DECLARE @amount DECIMAL(18,2) = @totalAmount - @amountPaid;

        INSERT INTO mn.Payment (Direction, PartyType, PartyID, ReferenceType, ReferenceID, Amount, Method, Notes, CreatedByUserID)
        VALUES (N'IN', N'CUSTOMER', @customerID, N'SALES_INVOICE', @invoiceID, @amount, N'Cash', N'Demo payment', 8);

        UPDATE mn.SalesInvoice SET AmountPaid = @amountPaid + @amount, Status = N'PAID' WHERE InvoiceID = @invoiceID;

        DECLARE @entryID INT;
        INSERT INTO mn.JournalEntry (ReferenceType, ReferenceID, Description) VALUES (N'PAYMENT', @invoiceID, N'Customer payment received');
        SET @entryID = SCOPE_IDENTITY();
        INSERT INTO mn.JournalLine (JournalEntryID, AccountID, Debit, Credit)
        SELECT @entryID, a.AccountID, l.Debit, l.Credit
        FROM (VALUES (N'1000', @amount, CAST(0 AS DECIMAL(18,2))),
                     (N'1100', CAST(0 AS DECIMAL(18,2)), @amount)) AS l(Code, Debit, Credit)
        JOIN mn.Account a ON a.AccountCode = l.Code;

        INSERT INTO mn.AuditLog (UserID, ActionCode, EntityType, EntityID, Details)
        VALUES (8, N'JOURNAL_POSTED', N'JournalEntry', @entryID, N'demo:payment-invoice');

        PRINT 'Batch 6: paid ' + CAST(@amount AS NVARCHAR(20)) + ' on invoice ' + CAST(@invoiceID AS NVARCHAR(10));
    END
END;
GO
