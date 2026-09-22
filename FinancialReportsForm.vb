Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms

Public Class FinancialReportsForm
    Inherits Form

    Private ReadOnly txtReport As New TextBox()

    Public Sub New()
        If Not SecurityContext.HasPermission("REPORT_VIEW") Then Throw New UnauthorizedAccessException("Your role cannot view financial reports.")
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Financial reports"
        Me.Size = New Size(720, 560)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Controls.Add(New Label With {.Text = "Financial reports", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .Location = New Point(24, 22), .AutoSize = True})
        txtReport.Location = New Point(24, 76)
        txtReport.Size = New Size(650, 390)
        txtReport.Multiline = True
        txtReport.ReadOnly = True
        txtReport.ScrollBars = ScrollBars.Vertical
        txtReport.Font = New Font("Consolas", 10)
        Me.Controls.Add(txtReport)
        Dim btnRefresh As Button = ModernTheme.MakePrimaryButton("Refresh")
        btnRefresh.Location = New Point(540, 480)
        btnRefresh.Width = 130
        AddHandler btnRefresh.Click, Sub() LoadReport()
        Me.Controls.Add(btnRefresh)
        LoadReport()
    End Sub

    Private Sub LoadReport()
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Dim revenue As Decimal = ScalarDecimal(conn, "SELECT ISNULL(SUM(Credit), 0) FROM mn.JournalLine jl JOIN mn.JournalEntry je ON je.JournalEntryID = jl.JournalEntryID JOIN mn.Account a ON a.AccountID = jl.AccountID WHERE a.AccountCode = '4000'")
                Dim cogs As Decimal = ScalarDecimal(conn, "SELECT ISNULL(SUM(Debit), 0) FROM mn.JournalLine jl JOIN mn.JournalEntry je ON je.JournalEntryID = jl.JournalEntryID JOIN mn.Account a ON a.AccountID = jl.AccountID WHERE a.AccountCode = '5000'")
                Dim expenses As Decimal = ScalarDecimal(conn, "SELECT ISNULL(SUM(Debit), 0) FROM mn.JournalLine jl JOIN mn.JournalEntry je ON je.JournalEntryID = jl.JournalEntryID JOIN mn.Account a ON a.AccountID = jl.AccountID WHERE a.AccountType = 'EXPENSE' AND a.AccountCode <> '5000'")
                Dim cash As Decimal = AccountBalance(conn, "1000")
                Dim receivables As Decimal = AccountBalance(conn, "1100")
                Dim inventory As Decimal = AccountBalance(conn, "1200")
                Dim payables As Decimal = AccountBalance(conn, "2000")
                Dim grossProfit As Decimal = revenue - cogs
                Dim netProfit As Decimal = grossProfit - expenses
                txtReport.Text = String.Format("PROFIT AND LOSS{0}{1}{0}Sales revenue:       {2,14:N2}{0}Cost of goods sold:  {3,14:N2}{0}Gross profit:        {4,14:N2}{0}Operating expenses:  {5,14:N2}{0}Net profit:          {6,14:N2}{0}{0}BALANCE SHEET{0}{1}{0}Cash:                {7,14:N2}{0}Accounts receivable:  {8,14:N2}{0}Inventory:            {9,14:N2}{0}Accounts payable:     {10,14:N2}{0}", Environment.NewLine, New String("-"c, 42), revenue, cogs, grossProfit, expenses, netProfit, cash, receivables, inventory, payables)
            End Using
        Catch ex As Exception
            txtReport.Text = "Unable to load financial report: " & ex.Message
        End Try
    End Sub

    Private Function ScalarDecimal(conn As SqlConnection, query As String) As Decimal
        Using cmd As New SqlCommand(query, conn)
            Return Convert.ToDecimal(cmd.ExecuteScalar())
        End Using
    End Function

    Private Function AccountBalance(conn As SqlConnection, accountCode As String) As Decimal
        Const query As String = "SELECT ISNULL(SUM(Debit - Credit), 0) FROM mn.JournalLine jl JOIN mn.Account a ON a.AccountID = jl.AccountID WHERE a.AccountCode = @code"
        Using cmd As New SqlCommand(query, conn)
            cmd.Parameters.AddWithValue("@code", accountCode)
            Return Convert.ToDecimal(cmd.ExecuteScalar())
        End Using
    End Function
End Class
