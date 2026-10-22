Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class OutstandingPayablesForm
    Inherits Form

    Private ReadOnly dgvReceipts As New DataGridView()
    Private ReadOnly lblTotalOutstanding As New Label()

    Public Sub New()
        If Not SecurityContext.HasPermission("REPORT_VIEW") Then
            Throw New UnauthorizedAccessException("Your role cannot view payables.")
        End If
        
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Outstanding payables"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.WindowState = FormWindowState.Maximized
        Me.AutoScroll = True

        Dim title As New Label With {
            .Text = "Outstanding payables",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .Location = New Point(24, 22),
            .AutoSize = True
        }
        Me.Controls.Add(title)

        dgvReceipts.Location = New Point(24, 76)
        dgvReceipts.Size = New Size(1000, 500)
        dgvReceipts.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        dgvReceipts.ReadOnly = True
        dgvReceipts.AllowUserToAddRows = False
        dgvReceipts.AllowUserToDeleteRows = False
        dgvReceipts.AllowUserToResizeRows = False
        dgvReceipts.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvReceipts.RowHeadersVisible = False
        ModernTheme.StyleGrid(dgvReceipts)

        dgvReceipts.Columns.Add("ReceiptID", "Receipt #")
        dgvReceipts.Columns.Add("SupplierName", "Supplier")
        dgvReceipts.Columns.Add("ReceiptDate", "Date")
        dgvReceipts.Columns.Add("TotalAmount", "Total")
        dgvReceipts.Columns.Add("AmountPaid", "Paid")
        dgvReceipts.Columns.Add("Balance", "Outstanding")
        dgvReceipts.Columns.Add("Status", "Status")
        dgvReceipts.Columns.Add("DaysOverdue", "Days Overdue")

        dgvReceipts.Columns("ReceiptID").Width = 80
        dgvReceipts.Columns("SupplierName").Width = 200
        dgvReceipts.Columns("ReceiptDate").Width = 100
        dgvReceipts.Columns("TotalAmount").Width = 120
        dgvReceipts.Columns("AmountPaid").Width = 120
        dgvReceipts.Columns("Balance").Width = 120
        dgvReceipts.Columns("Status").Width = 100
        dgvReceipts.Columns("DaysOverdue").Width = 100

        dgvReceipts.Columns("TotalAmount").DefaultCellStyle.Format = "N2"
        dgvReceipts.Columns("TotalAmount").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvReceipts.Columns("AmountPaid").DefaultCellStyle.Format = "N2"
        dgvReceipts.Columns("AmountPaid").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvReceipts.Columns("Balance").DefaultCellStyle.Format = "N2"
        dgvReceipts.Columns("Balance").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvReceipts.Columns("Balance").DefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        dgvReceipts.Columns("Balance").DefaultCellStyle.ForeColor = ModernTheme.Danger

        Me.Controls.Add(dgvReceipts)

        lblTotalOutstanding.Font = New Font("Segoe UI", 14, FontStyle.Bold)
        lblTotalOutstanding.ForeColor = ModernTheme.Danger
        lblTotalOutstanding.Location = New Point(24, 590)
        lblTotalOutstanding.AutoSize = True
        lblTotalOutstanding.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Me.Controls.Add(lblTotalOutstanding)

        Dim btnRefresh As Button = ModernTheme.MakePrimaryButton("Refresh")
        btnRefresh.Location = New Point(850, 590)
        btnRefresh.Width = 130
        btnRefresh.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler btnRefresh.Click, Sub() LoadReceipts()
        Me.Controls.Add(btnRefresh)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Close")
        btnClose.Location = New Point(710, 590)
        btnClose.Width = 130
        btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)

        AddHandler Me.Shown, Sub(s, e) LoadReceipts()
        LoadReceipts()
    End Sub

    Private Sub LoadReceipts()
        dgvReceipts.Rows.Clear()
        Dim totalOutstanding As Decimal = 0

        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Const sql As String = "SELECT pr.ReceiptID, s.SupplierName, pr.ReceiptDate, pr.TotalAmount, pr.AmountPaid, pr.Status, " &
                                      "DATEDIFF(DAY, pr.ReceiptDate, GETDATE()) AS DaysSinceReceipt " &
                                      "FROM mn.PurchaseReceipt pr " &
                                      "JOIN mn.Supplier s ON s.SupplierID = pr.SupplierID " &
                                      "WHERE pr.Status IN ('UNPAID', 'PARTIAL') " &
                                      "ORDER BY pr.ReceiptDate ASC"
                
                Using cmd As New SqlCommand(sql, conn)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim receiptID = reader.GetInt32(0)
                            Dim supplierName = reader.GetString(1)
                            Dim receiptDate = reader.GetDateTime(2).ToString("yyyy-MM-dd")
                            Dim totalAmount = reader.GetDecimal(3)
                            Dim amountPaid = reader.GetDecimal(4)
                            Dim status = reader.GetString(5)
                            Dim daysSinceReceipt = reader.GetInt32(6)
                            Dim balance = totalAmount - amountPaid

                            totalOutstanding += balance

                            Dim rowIdx = dgvReceipts.Rows.Add(receiptID, supplierName, receiptDate, totalAmount, amountPaid, balance, status, daysSinceReceipt)
                            
                            If daysSinceReceipt > 30 Then
                                dgvReceipts.Rows(rowIdx).DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235)
                            End If
                        End While
                    End Using
                End Using
            End Using

            lblTotalOutstanding.Text = $"Total outstanding: {totalOutstanding:N2} ETB"
        Catch ex As Exception
            AppLogger.Error("Failed to load outstanding payables", ex)
            MessageBox.Show("Unable to load payables: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
