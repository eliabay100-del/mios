Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class ReturnForm
    Inherits Form

    Private ReadOnly cmbType As New ComboBox()
    Private ReadOnly txtDocumentID As New TextBox()
    Private ReadOnly txtPartNumber As New TextBox()
    Private ReadOnly txtQuantity As New TextBox()
    Private ReadOnly txtAmount As New TextBox()
    Private ReadOnly txtReason As New TextBox()

    Public Sub New()
        If Not SecurityContext.HasPermission("RETURN_MANAGE") Then Throw New UnauthorizedAccessException("Your role cannot process returns.")
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Process return"
        Me.Size = New Size(590, 470)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Controls.Add(New Label With {.Text = "Process return", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .Location = New Point(28, 22), .AutoSize = True})
        AddField("Return type", cmbType, 75)
        AddField("Invoice / receipt ID", txtDocumentID, 120)
        AddField("Part number", txtPartNumber, 165)
        AddField("Quantity", txtQuantity, 210)
        AddField("Unit amount", txtAmount, 255)
        AddField("Reason", txtReason, 300)
        txtReason.Height = 55
        txtReason.Multiline = True
        cmbType.DropDownStyle = ComboBoxStyle.DropDownList
        cmbType.Items.AddRange(New Object() {"Customer return", "Supplier return"})
        cmbType.SelectedIndex = 0
        Dim btnSave As Button = ModernTheme.MakePrimaryButton("Save return")
        btnSave.Location = New Point(370, 390)
        btnSave.Width = 160
        AddHandler btnSave.Click, AddressOf SaveReturn_Click
        Me.Controls.Add(btnSave)
    End Sub

    Private Sub AddField(labelText As String, control As Control, top As Integer)
        Me.Controls.Add(New Label With {.Text = labelText, .Location = New Point(28, top + 5), .Size = New Size(140, 24), .ForeColor = ModernTheme.MutedText})
        control.Location = New Point(175, top)
        control.Width = 355
        Me.Controls.Add(control)
    End Sub

    Private Sub SaveReturn_Click(sender As Object, e As EventArgs)
        Dim documentID As Integer
        Dim quantity As Integer
        Dim amount As Decimal
        If Not Integer.TryParse(txtDocumentID.Text, documentID) OrElse documentID <= 0 OrElse String.IsNullOrWhiteSpace(txtPartNumber.Text) OrElse Not Integer.TryParse(txtQuantity.Text, quantity) OrElse quantity <= 0 OrElse Not Decimal.TryParse(txtAmount.Text, amount) OrElse amount < 0 OrElse String.IsNullOrWhiteSpace(txtReason.Text) Then
            MessageBox.Show("Enter a valid document, part, positive quantity, amount, and reason.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    If cmbType.SelectedIndex = 0 Then
                        ProcessSalesReturn(conn, transaction, documentID, quantity, amount)
                    Else
                        ProcessPurchaseReturn(conn, transaction, documentID, quantity, amount)
                    End If
                    transaction.Commit()
                End Using
            End Using
            MessageBox.Show("Return processed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.Close()
        Catch ex As Exception
            AppLogger.Error("Failed to process return", ex)
            MessageBox.Show("Unable to process return: " & ex.Message, "Return", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ProcessSalesReturn(conn As SqlConnection, transaction As SqlTransaction, invoiceID As Integer, quantity As Integer, amount As Decimal)
        Dim partNumber As String = txtPartNumber.Text.Trim()

        Dim customerID As Integer
        Using cmd As New SqlCommand("SELECT CustomerID FROM mn.SalesInvoice WHERE InvoiceID = @id", conn, transaction)
            cmd.Parameters.AddWithValue("@id", invoiceID)
            Dim value = cmd.ExecuteScalar()
            If value Is Nothing Then Throw New Exception("Sales invoice was not found.")
            customerID = Convert.ToInt32(value)
        End Using

        ' Cost basis of the returned units, taken from the original sale line when
        ' possible so the inventory/COGS reversal matches what was posted at sale.
        Dim unitCost As Decimal = GetSoldLineCost(conn, transaction, invoiceID, partNumber)

        Dim returnID As Integer
        Using cmd As New SqlCommand("INSERT INTO mn.SalesReturn (InvoiceID, CustomerID, PartNumber, Quantity, UnitPrice, Reason, CreatedByUserID) OUTPUT INSERTED.SalesReturnID VALUES (@invoice, @customer, @part, @quantity, @amount, @reason, @userID)", conn, transaction)
            AddCommonParameters(cmd, quantity, amount)
            cmd.Parameters.AddWithValue("@invoice", invoiceID)
            cmd.Parameters.AddWithValue("@customer", customerID)
            returnID = Convert.ToInt32(cmd.ExecuteScalar())
        End Using

        Using cmd As New SqlCommand("UPDATE mn.SpareParts SET Quantity = Quantity + @quantity WHERE PartNumber = @part", conn, transaction)
            cmd.Parameters.AddWithValue("@quantity", quantity)
            cmd.Parameters.AddWithValue("@part", partNumber)
            If cmd.ExecuteNonQuery() = 0 Then Throw New Exception("Part number was not found.")
        End Using

        ' Put the returned units back into the batch ledger so FIFO can issue them
        ' again; without this the stock exists only on the master quantity.
        InventoryService.AddBatch(conn, transaction, partNumber, quantity, unitCost, Nothing)

        AddStockMovement(conn, transaction, "IN", quantity, "Sales return")

        AccountingService.PostJournalEntry(conn, transaction, "SALES_RETURN", returnID, "Customer return", New List(Of JournalLineData) From {
            New JournalLineData With {.AccountCode = "4000", .Debit = quantity * amount, .Credit = 0},
            New JournalLineData With {.AccountCode = "1100", .Debit = 0, .Credit = quantity * amount},
            New JournalLineData With {.AccountCode = "1200", .Debit = quantity * unitCost, .Credit = 0},
            New JournalLineData With {.AccountCode = "5000", .Debit = 0, .Credit = quantity * unitCost}
        })
    End Sub

    Private Function GetSoldLineCost(conn As SqlConnection, transaction As SqlTransaction, invoiceID As Integer, partNumber As String) As Decimal
        Using cmd As New SqlCommand("SELECT TOP 1 UnitCost FROM mn.SalesInvoiceLine WHERE InvoiceID = @invoice AND PartNumber = @part ORDER BY InvoiceLineID DESC", conn, transaction)
            cmd.Parameters.AddWithValue("@invoice", invoiceID)
            cmd.Parameters.AddWithValue("@part", partNumber)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing AndAlso result IsNot DBNull.Value Then Return Convert.ToDecimal(result)
        End Using

        Using cmd As New SqlCommand("SELECT UnitPrice FROM mn.SpareParts WHERE PartNumber = @part", conn, transaction)
            cmd.Parameters.AddWithValue("@part", partNumber)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing AndAlso result IsNot DBNull.Value Then Return Convert.ToDecimal(result)
        End Using

        Return 0D
    End Function

    Private Sub ProcessPurchaseReturn(conn As SqlConnection, transaction As SqlTransaction, receiptID As Integer, quantity As Integer, amount As Decimal)
        Dim partNumber As String = txtPartNumber.Text.Trim()

        Dim supplierID As Integer
        Using cmd As New SqlCommand("SELECT SupplierID FROM mn.PurchaseReceipt WHERE ReceiptID = @id", conn, transaction)
            cmd.Parameters.AddWithValue("@id", receiptID)
            Dim value = cmd.ExecuteScalar()
            If value Is Nothing Then Throw New Exception("Purchase receipt was not found.")
            supplierID = Convert.ToInt32(value)
        End Using

        Dim returnID As Integer
        Using cmd As New SqlCommand("INSERT INTO mn.PurchaseReturn (ReceiptID, SupplierID, PartNumber, Quantity, UnitCost, Reason, CreatedByUserID) OUTPUT INSERTED.PurchaseReturnID VALUES (@receipt, @supplier, @part, @quantity, @amount, @reason, @userID)", conn, transaction)
            AddCommonParameters(cmd, quantity, amount)
            cmd.Parameters.AddWithValue("@receipt", receiptID)
            cmd.Parameters.AddWithValue("@supplier", supplierID)
            returnID = Convert.ToInt32(cmd.ExecuteScalar())
        End Using

        ' Reduce a single batch (never every matching batch) so the FIFO ledger
        ' is not over-consumed.
        If Not InventoryService.ReduceNewestBatch(conn, transaction, partNumber, quantity) Then
            Throw New Exception("Not enough remaining stock in a single purchase batch to return.")
        End If

        Using cmd As New SqlCommand("UPDATE mn.SpareParts SET Quantity = Quantity - @quantity WHERE PartNumber = @part AND Quantity >= @quantity", conn, transaction)
            cmd.Parameters.AddWithValue("@quantity", quantity)
            cmd.Parameters.AddWithValue("@part", partNumber)
            If cmd.ExecuteNonQuery() = 0 Then Throw New Exception("Part stock could not be reduced.")
        End Using

        AddStockMovement(conn, transaction, "OUT", quantity, "Purchase return")

        AccountingService.PostJournalEntry(conn, transaction, "PURCHASE_RETURN", returnID, "Supplier return", New List(Of JournalLineData) From {
            New JournalLineData With {.AccountCode = "2000", .Debit = quantity * amount, .Credit = 0},
            New JournalLineData With {.AccountCode = "1200", .Debit = 0, .Credit = quantity * amount}
        })
    End Sub

    Private Sub AddCommonParameters(cmd As SqlCommand, quantity As Integer, amount As Decimal)
        cmd.Parameters.AddWithValue("@part", txtPartNumber.Text.Trim())
        cmd.Parameters.AddWithValue("@quantity", quantity)
        cmd.Parameters.AddWithValue("@amount", amount)
        cmd.Parameters.AddWithValue("@reason", txtReason.Text.Trim())
        cmd.Parameters.AddWithValue("@userID", If(SecurityContext.CurrentUserID = 0, DBNull.Value, CObj(SecurityContext.CurrentUserID)))
    End Sub

    Private Sub AddStockMovement(conn As SqlConnection, transaction As SqlTransaction, movement As String, quantity As Integer, reference As String)
        Using cmd As New SqlCommand("INSERT INTO mn.StockCard (PartNumber, MovementType, Quantity, ReferenceType, Remarks) VALUES (@part, @movement, @quantity, @reference, @reason)", conn, transaction)
            cmd.Parameters.AddWithValue("@part", txtPartNumber.Text.Trim())
            cmd.Parameters.AddWithValue("@movement", movement)
            cmd.Parameters.AddWithValue("@quantity", quantity)
            cmd.Parameters.AddWithValue("@reference", reference)
            cmd.Parameters.AddWithValue("@reason", txtReason.Text.Trim())
            cmd.ExecuteNonQuery()
        End Using
    End Sub
End Class
