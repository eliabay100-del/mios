Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms

Public Class FinancialReportsForm
    Inherits Form

    Private ReadOnly dgvReport As New DataGridView()

    Public Sub New()
        If Not SecurityContext.HasPermission("REPORT_VIEW") Then Throw New UnauthorizedAccessException("Your role cannot view financial reports.")
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Financial reports"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.WindowState = FormWindowState.Maximized
        Me.AutoScroll = True

        Dim title As New Label With {.Text = "Financial reports", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .Location = New Point(24, 22), .AutoSize = True}
        Me.Controls.Add(title)

        dgvReport.Location = New Point(24, 76)
        dgvReport.Size = New Size(650, 440)
        dgvReport.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        dgvReport.ReadOnly = True
        dgvReport.AllowUserToAddRows = False
        dgvReport.AllowUserToDeleteRows = False
        dgvReport.AllowUserToResizeRows = False
        dgvReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgvReport.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvReport.RowHeadersVisible = False
        dgvReport.ColumnHeadersVisible = False
        ' Use the shared grid look so this report matches every other table.
        ModernTheme.StyleGrid(dgvReport)

        Dim colLabel As New DataGridViewTextBoxColumn()
        colLabel.Name = "Label"
        colLabel.HeaderText = "Label"
        colLabel.Width = 320
        ' Fill the grid so the report columns use the whole width at any DPI,
        ' instead of leaving a blank strip on the right.
        colLabel.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        colLabel.FillWeight = 62.0F
        colLabel.DefaultCellStyle.Font = New Font("Segoe UI", 10)
        dgvReport.Columns.Add(colLabel)

        Dim colAmount As New DataGridViewTextBoxColumn()
        colAmount.Name = "Amount"
        colAmount.HeaderText = "Amount"
        colAmount.Width = 180
        colAmount.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        colAmount.FillWeight = 38.0F
        colAmount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        colAmount.DefaultCellStyle.Font = New Font("Consolas", 10)
        colAmount.DefaultCellStyle.Format = "N2"
        dgvReport.Columns.Add(colAmount)

        Dim colSection As New DataGridViewTextBoxColumn()
        colSection.Name = "Section"
        colSection.Visible = False
        dgvReport.Columns.Add(colSection)

        Me.Controls.Add(dgvReport)

        Dim btnRefresh As Button = ModernTheme.MakePrimaryButton("Refresh")
        btnRefresh.Location = New Point(540, 530)
        btnRefresh.Width = 130
        btnRefresh.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler btnRefresh.Click, Sub() LoadReport()
        Me.Controls.Add(btnRefresh)

        Dim btnExport As Button = ModernTheme.MakeSecondaryButton("Export CSV")
        btnExport.Location = New Point(400, 530)
        btnExport.Width = 130
        btnExport.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler btnExport.Click, Sub() ExportCSV()
        Me.Controls.Add(btnExport)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Close")
        btnClose.Location = New Point(260, 530)
        btnClose.Width = 130
        btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)

        Me.CancelButton = btnClose

        LoadReport()
    End Sub

    Private Sub LoadReport()
        dgvReport.Rows.Clear()
        Me.Cursor = Cursors.WaitCursor
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

                AddSectionHeader("PROFIT AND LOSS")
                AddDataRow("Sales revenue", revenue)
                AddDataRow("Cost of goods sold", cogs)
                AddDataRow("Gross profit", grossProfit, True)
                AddDataRow("Operating expenses", expenses)
                AddDataRow("Net profit", netProfit, True)
                AddSpacer()
                AddSectionHeader("BALANCE SHEET")
                AddDataRow("Cash", cash)
                AddDataRow("Accounts receivable", receivables)
                AddDataRow("Inventory", inventory)
                AddDataRow("Accounts payable", payables)

                AppLogger.Info("Financial reports loaded")
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load financial reports", ex)
            MessageBox.Show("Unable to load financial report: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub AddSectionHeader(text As String)
        Dim idx = dgvReport.Rows.Add(text, "", "HEADER")
        Dim row = dgvReport.Rows(idx)
        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        row.Cells(0).Style.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        row.Cells(1).Style.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        row.DefaultCellStyle.ForeColor = ModernTheme.PrimaryText
    End Sub

    Private Sub AddDataRow(label As String, amount As Decimal, Optional bold As Boolean = False)
        Dim idx = dgvReport.Rows.Add(label, amount, "DATA")
        Dim row = dgvReport.Rows(idx)
        If bold Then
            row.Cells(0).Style.Font = New Font("Segoe UI", 10, FontStyle.Bold)
            row.Cells(1).Style.Font = New Font("Consolas", 10, FontStyle.Bold)
        End If
    End Sub

    Private Sub AddSpacer()
        Dim idx = dgvReport.Rows.Add("", "", "SPACER")
        dgvReport.Rows(idx).Height = 10
        dgvReport.Rows(idx).DefaultCellStyle.BackColor = Color.White
    End Sub

    Private Sub ExportCSV()
        If dgvReport.Rows.Count = 0 Then Return
        Using dialog As New SaveFileDialog()
            dialog.Filter = "CSV files (*.csv)|*.csv"
            dialog.FileName = "Financial_Report_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".csv"
            If dialog.ShowDialog() <> DialogResult.OK Then Return

            Try
                Dim sb As New StringBuilder()
                For Each row As DataGridViewRow In dgvReport.Rows
                    If row.Cells(2).Value?.ToString() = "SPACER" Then Continue For
                    sb.AppendLine(row.Cells(0).Value.ToString() & "," & row.Cells(1).Value.ToString())
                Next
                IO.File.WriteAllText(dialog.FileName, sb.ToString())
                AppLogger.Info("Financial report exported to CSV: " & dialog.FileName)
                MessageBox.Show("Report exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                AppLogger.Error("Failed to export financial report", ex)
                MessageBox.Show("Export failed: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
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
