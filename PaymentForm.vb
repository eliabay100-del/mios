Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class PaymentForm
    Inherits Form

    Private ReadOnly cmbDocumentType As New ComboBox()
    Private ReadOnly txtDocumentID As New TextBox()
    Private ReadOnly txtAmount As New TextBox()
    Private ReadOnly cmbMethod As New ComboBox()
    Private ReadOnly txtNotes As New TextBox()
    Private ReadOnly lblError As New Label()

    Public Sub New()
        If Not SecurityContext.HasPermission("PAYMENT_MANAGE") Then
            Throw New UnauthorizedAccessException("Your role cannot record payments.")
        End If
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Record payment"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.WindowState = FormWindowState.Maximized
        Me.AutoScroll = True

        Dim title As New Label With {
            .Text = "Record payment",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .Location = New Point(30, 24),
            .AutoSize = True
        }
        Me.Controls.Add(title)

        AddField("Document type", cmbDocumentType, 82)
        cmbDocumentType.DropDownStyle = ComboBoxStyle.DropDownList
        cmbDocumentType.Items.AddRange(New Object() {"Sales invoice", "Purchase receipt"})
        cmbDocumentType.SelectedIndex = 0

        AddField("Document ID", txtDocumentID, 132)
        AddField("Amount", txtAmount, 182)
        AddField("Payment method", cmbMethod, 232)
        cmbMethod.DropDownStyle = ComboBoxStyle.DropDownList
        cmbMethod.Items.AddRange(New Object() {"Cash", "Bank transfer", "Card", "Other"})
        cmbMethod.SelectedIndex = 0

        AddField("Notes", txtNotes, 282)
        txtNotes.Height = 45
        txtNotes.Multiline = True

        lblError.ForeColor = ModernTheme.Danger
        lblError.Location = New Point(180, 340)
        lblError.Size = New Size(330, 28)
        Me.Controls.Add(lblError)

        Dim btnSave As Button = ModernTheme.MakePrimaryButton("Save payment")
        btnSave.Location = New Point(345, 375)
        btnSave.Width = 160
        AddHandler btnSave.Click, AddressOf SavePayment_Click
        Me.Controls.Add(btnSave)

        Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("Cancel")
        btnCancel.Location = New Point(180, 375)
        btnCancel.Width = 150
        AddHandler btnCancel.Click, Sub() Me.Close()
        Me.Controls.Add(btnCancel)

        Me.AcceptButton = btnSave
        Me.CancelButton = btnCancel
    End Sub

    Private Sub AddField(labelText As String, control As Control, top As Integer)
        Me.Controls.Add(New Label With {
            .Text = labelText,
            .Location = New Point(30, top + 5),
            .Size = New Size(140, 24),
            .ForeColor = ModernTheme.MutedText,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        })
        control.Location = New Point(180, top)
        control.Width = 330
        If TypeOf control Is TextBox Then DirectCast(control, TextBox).Height = 30
        Me.Controls.Add(control)
    End Sub

    Private Sub SavePayment_Click(sender As Object, e As EventArgs)
        lblError.Text = ""
        Dim documentID As Integer
        Dim amount As Decimal

        If Not Integer.TryParse(txtDocumentID.Text.Trim(), documentID) OrElse documentID <= 0 Then
            lblError.Text = "Enter a valid document ID."
            Return
        End If
        If Not Decimal.TryParse(txtAmount.Text.Trim(), amount) OrElse amount <= 0 Then
            lblError.Text = "Enter a valid positive amount."
            Return
        End If

        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using transaction As SqlTransaction = conn.BeginTransaction()
                    Dim isSales As Boolean = cmbDocumentType.SelectedIndex = 0
                    Dim partyID As Integer
                    Dim amountPaid As Decimal
                    Dim totalAmount As Decimal
                    Dim referenceType As String
                    Dim partyType As String
                    Dim direction As String

                    If isSales Then
                        referenceType = "SALES_INVOICE"
                        partyType = "CUSTOMER"
                        direction = "IN"
                        Using cmd As New SqlCommand("SELECT CustomerID, TotalAmount, AmountPaid FROM mn.SalesInvoice WHERE InvoiceID = @id", conn, transaction)
                            cmd.Parameters.AddWithValue("@id", documentID)
                            Using reader = cmd.ExecuteReader()
                                If Not reader.Read() Then Throw New Exception("Sales invoice was not found.")
                                partyID = Convert.ToInt32(reader("CustomerID"))
                                totalAmount = Convert.ToDecimal(reader("TotalAmount"))
                                amountPaid = Convert.ToDecimal(reader("AmountPaid"))
                            End Using
                        End Using
                    Else
                        referenceType = "PURCHASE_RECEIPT"
                        partyType = "SUPPLIER"
                        direction = "OUT"
                        Using cmd As New SqlCommand("SELECT SupplierID, TotalAmount, AmountPaid FROM mn.PurchaseReceipt WHERE ReceiptID = @id", conn, transaction)
                            cmd.Parameters.AddWithValue("@id", documentID)
                            Using reader = cmd.ExecuteReader()
                                If Not reader.Read() Then Throw New Exception("Purchase receipt was not found.")
                                partyID = Convert.ToInt32(reader("SupplierID"))
                                totalAmount = Convert.ToDecimal(reader("TotalAmount"))
                                amountPaid = Convert.ToDecimal(reader("AmountPaid"))
                            End Using
                        End Using
                    End If

                    If amount > totalAmount - amountPaid Then Throw New Exception("Payment exceeds the outstanding balance.")
                    Dim newAmountPaid As Decimal = amountPaid + amount
                    Dim status As String = If(newAmountPaid = totalAmount, "PAID", "PARTIAL")

                    Dim paymentSql As String = "INSERT INTO mn.Payment (Direction, PartyType, PartyID, ReferenceType, ReferenceID, Amount, Method, Notes) VALUES (@direction, @partyType, @partyID, @referenceType, @referenceID, @amount, @method, @notes)"
                    Using cmd As New SqlCommand(paymentSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@direction", direction)
                        cmd.Parameters.AddWithValue("@partyType", partyType)
                        cmd.Parameters.AddWithValue("@partyID", partyID)
                        cmd.Parameters.AddWithValue("@referenceType", referenceType)
                        cmd.Parameters.AddWithValue("@referenceID", documentID)
                        cmd.Parameters.AddWithValue("@amount", amount)
                        cmd.Parameters.AddWithValue("@method", cmbMethod.Text)
                        cmd.Parameters.AddWithValue("@notes", If(String.IsNullOrWhiteSpace(txtNotes.Text), DBNull.Value, CObj(txtNotes.Text.Trim())))
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim updateSql As String
                    If isSales Then
                        updateSql = "UPDATE mn.SalesInvoice SET AmountPaid = @paid, Status = @status WHERE InvoiceID = @id"
                    Else
                        updateSql = "UPDATE mn.PurchaseReceipt SET AmountPaid = @paid, Status = @status WHERE ReceiptID = @id"
                    End If
                    Using cmd As New SqlCommand(updateSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@paid", newAmountPaid)
                        cmd.Parameters.AddWithValue("@status", status)
                        cmd.Parameters.AddWithValue("@id", documentID)
                        cmd.ExecuteNonQuery()
                    End Using

                    If isSales Then
                        AccountingService.PostJournalEntry(conn, transaction, "PAYMENT", documentID, "Customer payment received", New List(Of JournalLineData) From {
                            New JournalLineData With {.AccountCode = "1000", .Debit = amount, .Credit = 0},
                            New JournalLineData With {.AccountCode = "1100", .Debit = 0, .Credit = amount}
                        })
                    Else
                        AccountingService.PostJournalEntry(conn, transaction, "PAYMENT", documentID, "Supplier payment made", New List(Of JournalLineData) From {
                            New JournalLineData With {.AccountCode = "2000", .Debit = amount, .Credit = 0},
                            New JournalLineData With {.AccountCode = "1000", .Debit = 0, .Credit = amount}
                        })
                    End If

                    transaction.Commit()
                End Using
            End Using

            MessageBox.Show("Payment recorded successfully.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.Close()
        Catch ex As Exception
            AppLogger.Error("Failed to record payment", ex)
            lblError.Text = "Unable to record the payment. Please check the document and amount."
        End Try
    End Sub
End Class
