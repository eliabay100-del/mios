Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class ExpenseForm
    Inherits Form

    Private ReadOnly cmbAccount As New ComboBox()
    Private ReadOnly txtAmount As New TextBox()
    Private ReadOnly txtDescription As New TextBox()
    Private ReadOnly cmbMethod As New ComboBox()

    Public Sub New()
        If Not SecurityContext.HasPermission("EXPENSE_MANAGE") Then Throw New UnauthorizedAccessException("Your role cannot record expenses.")
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Record expense"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.WindowState = FormWindowState.Maximized
        Me.AutoScroll = True
        Me.Controls.Add(New Label With {.Text = "Record expense", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .Location = New Point(28, 24), .AutoSize = True})
        AddField("Expense account", cmbAccount, 90)
        AddField("Amount", txtAmount, 140)
        AddField("Description", txtDescription, 190)
        AddField("Payment method", cmbMethod, 240)
        cmbAccount.DropDownStyle = ComboBoxStyle.DropDownList
        cmbMethod.DropDownStyle = ComboBoxStyle.DropDownList
        cmbMethod.Items.AddRange(New Object() {"Cash", "Bank transfer", "Card", "Other"})
        cmbMethod.SelectedIndex = 0
        LoadAccounts()
        Dim btnSave As Button = ModernTheme.MakePrimaryButton("Save expense")
        btnSave.Location = New Point(345, 320)
        btnSave.Width = 160
        AddHandler btnSave.Click, AddressOf SaveExpense_Click
        Me.Controls.Add(btnSave)

        Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("Cancel")
        btnCancel.Location = New Point(180, 320)
        btnCancel.Width = 150
        AddHandler btnCancel.Click, Sub() Me.Close()
        Me.Controls.Add(btnCancel)

        Me.AcceptButton = btnSave
        Me.CancelButton = btnCancel
    End Sub

    Private Sub AddField(labelText As String, control As Control, top As Integer)
        Me.Controls.Add(New Label With {.Text = labelText, .Location = New Point(28, top + 5), .Size = New Size(125, 24), .ForeColor = ModernTheme.MutedText})
        control.Location = New Point(160, top)
        control.Width = 350
        Me.Controls.Add(control)
    End Sub

    Private Sub LoadAccounts()
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                ' Cost of Goods Sold (5000) is driven by inventory issues, not manual
                ' expenses, and the financial report excludes it from operating
                ' expenses - so keep it out of this picker to avoid double counting.
                Using cmd As New SqlCommand("SELECT AccountID, AccountCode, AccountName FROM mn.Account WHERE AccountType = 'EXPENSE' AND AccountCode <> '5000' ORDER BY AccountCode", conn)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            cmbAccount.Items.Add(New AccountItem With {.ID = reader.GetInt32(0), .Code = reader.GetString(1), .Name = reader.GetString(2)})
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load expense accounts", ex)
            MessageBox.Show("Unable to load expense accounts: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        If cmbAccount.Items.Count > 0 Then cmbAccount.SelectedIndex = 0
    End Sub

    Private Class AccountItem
        Public Property ID As Integer
        Public Property Code As String
        Public Property Name As String
        Public Overrides Function ToString() As String
            Return Code & " - " & Name
        End Function
    End Class

    Private Sub SaveExpense_Click(sender As Object, e As EventArgs)
        Dim amount As Decimal
        If cmbAccount.SelectedItem Is Nothing OrElse Not Decimal.TryParse(txtAmount.Text, amount) OrElse amount <= 0 OrElse String.IsNullOrWhiteSpace(txtDescription.Text) Then
            MessageBox.Show("Select an expense account and enter a positive amount and description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Dim account = DirectCast(cmbAccount.SelectedItem, AccountItem)
                    Dim expenseID As Integer
                    Using cmd As New SqlCommand("INSERT INTO mn.Expense (ExpenseAccountID, Amount, Description, PaymentMethod, CreatedByUserID) OUTPUT INSERTED.ExpenseID VALUES (@accountID, @amount, @description, @method, @userID)", conn, transaction)
                        cmd.Parameters.AddWithValue("@accountID", account.ID)
                        cmd.Parameters.AddWithValue("@amount", amount)
                        cmd.Parameters.AddWithValue("@description", txtDescription.Text.Trim())
                        cmd.Parameters.AddWithValue("@method", cmbMethod.Text)
                        cmd.Parameters.AddWithValue("@userID", If(SecurityContext.CurrentUserID = 0, DBNull.Value, CObj(SecurityContext.CurrentUserID)))
                        expenseID = Convert.ToInt32(cmd.ExecuteScalar())
                    End Using
                    AccountingService.PostJournalEntry(conn, transaction, "EXPENSE", expenseID, txtDescription.Text.Trim(), New List(Of JournalLineData) From {
                        New JournalLineData With {.AccountCode = account.Code, .Debit = amount, .Credit = 0},
                        New JournalLineData With {.AccountCode = "1000", .Debit = 0, .Credit = amount}
                    })
                    transaction.Commit()
                End Using
            End Using
            MessageBox.Show("Expense recorded.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.Close()
        Catch ex As Exception
            AppLogger.Error("Failed to record expense", ex)
            MessageBox.Show("Unable to save expense: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
