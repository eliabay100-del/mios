Imports System.Data.SqlClient

''' <summary>
''' Keeps the master inventory quantity (mn.SpareParts.Quantity) and the
''' purchase-batch ledger (mn.PurchaseBatch.QuantityRemaining) in step.
'''
''' SpareParts.Quantity is what the UI shows; PurchaseBatch.QuantityRemaining
''' is what FIFO issues from. Every code path that changes stock must touch
''' both, otherwise returned/adjusted stock becomes un-issuable or batches
''' under-report what is on the shelf.
''' </summary>
Public Module InventoryService

    ''' <summary>
    ''' Adds a purchase-style batch so FIFO can consume the stock again. Used for
    ''' normal receiving, customer returns, and upward inventory adjustments.
    ''' </summary>
    Public Sub AddBatch(conn As SqlConnection, transaction As SqlTransaction, partNumber As String, quantity As Integer, unitCost As Decimal, supplierID As Integer?)
        If quantity <= 0 Then Return

        Const sql As String = "INSERT INTO mn.PurchaseBatch " &
                              "(PartNumber, SupplierID, PurchaseDate, QuantityPurchased, QuantityRemaining, UnitCost) " &
                              "VALUES (@part, @supplier, GETDATE(), @qty, @qty, @cost)"
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@part", partNumber)
            cmd.Parameters.AddWithValue("@supplier", If(supplierID.HasValue AndAlso supplierID.Value > 0, CObj(supplierID.Value), DBNull.Value))
            cmd.Parameters.AddWithValue("@qty", quantity)
            cmd.Parameters.AddWithValue("@cost", unitCost)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Reduces batch remaining quantities, oldest first, until the requested
    ''' quantity has been removed. Throws when the batches do not hold enough.
    ''' </summary>
    Public Sub ReduceBatchesFifo(conn As SqlConnection, transaction As SqlTransaction, partNumber As String, quantity As Integer)
        If quantity <= 0 Then Return

        Dim plan As List(Of Tuple(Of Integer, Integer)) = PlanReduction(conn, transaction, partNumber, quantity)
        Dim removed As Integer = 0
        For Each item As Tuple(Of Integer, Integer) In plan
            removed += item.Item2
        Next

        If removed < quantity Then
            Throw New InvalidOperationException("Not enough purchase-batch stock remaining for " & partNumber & ".")
        End If

        ApplyReduction(conn, transaction, plan)
    End Sub

    ''' <summary>
    ''' Reduces batches by as much as is available without throwing. Used for
    ''' downward inventory adjustments where the master quantity is authoritative.
    ''' </summary>
    Public Sub ReduceBatchesUpTo(conn As SqlConnection, transaction As SqlTransaction, partNumber As String, quantity As Integer)
        If quantity <= 0 Then Return
        ApplyReduction(conn, transaction, PlanReduction(conn, transaction, partNumber, quantity))
    End Sub

    ''' <summary>
    ''' Reduces a single purchase batch (the most recent with enough remaining).
    ''' Returns False when no single batch can absorb the quantity. Used for
    ''' supplier returns, which must not over-reduce every matching batch.
    ''' </summary>
    Public Function ReduceNewestBatch(conn As SqlConnection, transaction As SqlTransaction, partNumber As String, quantity As Integer) As Boolean
        If quantity <= 0 Then Return True

        Dim batchID As Integer = -1
        Const selectSql As String = "SELECT TOP 1 BatchID FROM mn.PurchaseBatch WITH (UPDLOCK, ROWLOCK) " &
                                    "WHERE PartNumber = @part AND QuantityRemaining >= @qty " &
                                    "ORDER BY PurchaseDate DESC, BatchID DESC"
        Using cmd As New SqlCommand(selectSql, conn, transaction)
            cmd.Parameters.AddWithValue("@part", partNumber)
            cmd.Parameters.AddWithValue("@qty", quantity)
            Dim result As Object = cmd.ExecuteScalar()
            If result Is Nothing OrElse result Is DBNull.Value Then Return False
            batchID = Convert.ToInt32(result)
        End Using

        Using cmd As New SqlCommand("UPDATE mn.PurchaseBatch SET QuantityRemaining = QuantityRemaining - @qty WHERE BatchID = @id", conn, transaction)
            cmd.Parameters.AddWithValue("@qty", quantity)
            cmd.Parameters.AddWithValue("@id", batchID)
            cmd.ExecuteNonQuery()
        End Using
        Return True
    End Function

    ''' <summary>Applies an inventory adjustment to the batch ledger.</summary>
    Public Sub ApplyAdjustment(conn As SqlConnection, transaction As SqlTransaction, partNumber As String, difference As Integer, unitCost As Decimal)
        If difference > 0 Then
            AddBatch(conn, transaction, partNumber, difference, unitCost, Nothing)
        ElseIf difference < 0 Then
            ReduceBatchesUpTo(conn, transaction, partNumber, -difference)
        End If
    End Sub

    Private Function PlanReduction(conn As SqlConnection, transaction As SqlTransaction, partNumber As String, quantity As Integer) As List(Of Tuple(Of Integer, Integer))
        Dim remaining As Integer = quantity
        Dim plan As New List(Of Tuple(Of Integer, Integer))()

        Const selectSql As String = "SELECT BatchID, QuantityRemaining FROM mn.PurchaseBatch WITH (UPDLOCK, ROWLOCK) " &
                                    "WHERE PartNumber = @part AND QuantityRemaining > 0 " &
                                    "ORDER BY PurchaseDate ASC, BatchID ASC"
        Using cmd As New SqlCommand(selectSql, conn, transaction)
            cmd.Parameters.AddWithValue("@part", partNumber)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While remaining > 0 AndAlso reader.Read()
                    Dim batchID As Integer = reader.GetInt32(0)
                    Dim available As Integer = reader.GetInt32(1)
                    Dim take As Integer = Math.Min(remaining, available)
                    If take > 0 Then
                        plan.Add(Tuple.Create(batchID, take))
                        remaining -= take
                    End If
                End While
            End Using
        End Using

        Return plan
    End Function

    Private Sub ApplyReduction(conn As SqlConnection, transaction As SqlTransaction, plan As List(Of Tuple(Of Integer, Integer)))
        For Each item As Tuple(Of Integer, Integer) In plan
            Using cmd As New SqlCommand("UPDATE mn.PurchaseBatch SET QuantityRemaining = QuantityRemaining - @qty WHERE BatchID = @id", conn, transaction)
                cmd.Parameters.AddWithValue("@qty", item.Item2)
                cmd.Parameters.AddWithValue("@id", item.Item1)
                cmd.ExecuteNonQuery()
            End Using
        Next
    End Sub

End Module
