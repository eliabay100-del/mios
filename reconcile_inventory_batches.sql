/* ============================================================================
   reconcile_inventory_batches.sql
   ----------------------------------------------------------------------------
   MIOS keeps two stock figures:

       mn.SpareParts.Quantity          <- what the UI shows
       mn.PurchaseBatch.QuantityRemaining <- what FIFO issues from (summed)

   Historically some code paths (customer returns, manual inventory edits,
   stock adjustments) changed one but not the other, which left stock that
   could never be issued, or batches that over-reported what was on the shelf.

   The application now keeps them in step (see App\InventoryService.vb).
   Run this script to (1) find any existing divergence and (2) optionally
   repair it.

   >>> STEP 1 is read-only. Review the output before running STEP 2. <<<
   ============================================================================ */

USE [Inventory];
GO

/* ---------------------------------------------------------------------------
   STEP 1 - REPORT: parts where the master quantity and the batch ledger differ
   --------------------------------------------------------------------------- */
SELECT
    sp.PartNumber,
    sp.Quantity                                   AS MasterQuantity,
    ISNULL(b.BatchRemaining, 0)                   AS BatchRemaining,
    sp.Quantity - ISNULL(b.BatchRemaining, 0)     AS Difference,
    CASE
        WHEN sp.Quantity > ISNULL(b.BatchRemaining, 0) THEN 'Master higher: stock is NOT issuable by FIFO'
        ELSE 'Batches higher: FIFO can over-issue'
    END                                           AS Problem
FROM mn.SpareParts AS sp
LEFT JOIN (
    SELECT PartNumber, SUM(QuantityRemaining) AS BatchRemaining
    FROM mn.PurchaseBatch
    GROUP BY PartNumber
) AS b ON b.PartNumber = sp.PartNumber
WHERE sp.Quantity <> ISNULL(b.BatchRemaining, 0)
ORDER BY ABS(sp.Quantity - ISNULL(b.BatchRemaining, 0)) DESC;
GO

/* ---------------------------------------------------------------------------
   STEP 2 - REPAIR (optional, transactional)

   * Parts where the master quantity is higher get a synthetic batch for the
     difference, priced at the current unit price, so the stock becomes
     issuable again.
   * Parts where the batches are higher are trimmed oldest-first (FIFO) until
     the batch total matches the master quantity.

   Uncomment the block below when you are ready to apply the fix.
   --------------------------------------------------------------------------- */
/*
BEGIN TRANSACTION;

-- 2a. Master higher than batches -> create a balancing batch.
INSERT INTO mn.PurchaseBatch
    (PartNumber, SupplierID, PurchaseDate, QuantityPurchased, QuantityRemaining, UnitCost)
SELECT
    sp.PartNumber,
    NULL,
    GETDATE(),
    sp.Quantity - ISNULL(b.BatchRemaining, 0),
    sp.Quantity - ISNULL(b.BatchRemaining, 0),
    sp.UnitPrice
FROM mn.SpareParts AS sp
LEFT JOIN (
    SELECT PartNumber, SUM(QuantityRemaining) AS BatchRemaining
    FROM mn.PurchaseBatch
    GROUP BY PartNumber
) AS b ON b.PartNumber = sp.PartNumber
WHERE sp.Quantity > ISNULL(b.BatchRemaining, 0);

-- 2b. Batches higher than master -> trim oldest batches (FIFO) to match.
;WITH Excess AS (
    SELECT sp.PartNumber, ISNULL(b.BatchRemaining, 0) - sp.Quantity AS Excess
    FROM mn.SpareParts AS sp
    LEFT JOIN (
        SELECT PartNumber, SUM(QuantityRemaining) AS BatchRemaining
        FROM mn.PurchaseBatch
        GROUP BY PartNumber
    ) AS b ON b.PartNumber = sp.PartNumber
    WHERE ISNULL(b.BatchRemaining, 0) > sp.Quantity
),
Ordered AS (
    SELECT
        pb.BatchID,
        pb.QuantityRemaining,
        e.Excess,
        SUM(pb.QuantityRemaining) OVER (
            PARTITION BY pb.PartNumber
            ORDER BY pb.PurchaseDate ASC, pb.BatchID ASC
            ROWS UNBOUNDED PRECEDING
        ) AS RunningTotal
    FROM mn.PurchaseBatch AS pb
    JOIN Excess AS e ON e.PartNumber = pb.PartNumber
)
UPDATE Ordered
SET QuantityRemaining = CASE
        WHEN RunningTotal <= Excess THEN 0
        WHEN RunningTotal - QuantityRemaining < Excess THEN RunningTotal - Excess
        ELSE QuantityRemaining
    END
WHERE RunningTotal > 0;

-- Re-run STEP 1; it should return no rows before you COMMIT.
-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;
*/
GO
