Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class OutstandingReceivablesForm
    Inherits Form

    Private ReadOnly dgvInvoices As New DataGridView()
    Private ReadOnly lblTotalOutstanding As New Label()

    Public Sub New()
        If Not SecurityContext.HasPermission("REPORT_VIEW") Then
            Throw New UnauthorizedAccessException("Your role cannot view receivables.")
        End If
        
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Outstanding receivables"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.WindowState = FormWindowState.Maximized
        Me.AutoScroll = True

        Dim title As New Label With {
            .Text = "Outstanding receivables",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .Location = New Point(24, 22),
            .AutoSize = True
        }
        Me.Controls.Add(title)

        dgvInvoices.Location = New Point(24, 76)
        dgvInvoices.Size = New Size(1000, 500)
        dgvInvoices.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        dgvInvoices.ReadOnly = True
        dgvInvoices.AllowUserToAddRows = False
        dgvInvoices.AllowUserToDeleteRows = False
        dgvInvoices.AllowUserToResizeRows = False
        dgvInvoices.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvInvoices.RowHeadersVisible = False
        ModernTheme.StyleGrid(dgvInvoices)

        dgvInvoices.Columns.Add("InvoiceID", "Invoice #")
        dgvInvoices.Columns.Add("CustomerName", "Customer")
        dgvInvoices.Columns.Add("InvoiceDate", "Date")
        dgvInvoices.Columns.Add("TotalAmount", "Total")
        dgvInvoices.Columns.Add("AmountPaid", "Paid")
        dgvInvoices.Columns.Add("Balance", "Outstanding")
        dgvInvoices.Columns.Add("Status", "Status")
        dgvInvoices.Columns.Add("DaysOverdue", "Days Overdue")

        dgvInvoices.Columns("InvoiceID").Width = 80
        dgvInvoices.Columns("CustomerName").Width = 200
        dgvInvoices.Columns("InvoiceDate").Width = 100
        dgvInvoices.Columns("TotalAmount").Width = 120
        dgvInvoices.Columns("AmountPaid").Width = 120
        dgvInvoices.Columns("Balance").Width = 120
        dgvInvoices.Columns("Status").Width = 100
        dgvInvoices.Columns("DaysOverdue").Width = 100

        dgvInvoices.Columns("TotalAmount").DefaultCellStyle.Format = "N2"
        dgvInvoices.Columns("TotalAmount").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvInvoices.Columns("AmountPaid").DefaultCellStyle.Format = "N2"
        dgvInvoices.Columns("AmountPaid").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvInvoices.Columns("Balance").DefaultCellStyle.Format = "N2"
        dgvInvoices.Columns("Balance").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvInvoices.Columns("Balance").DefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        dgvInvoices.Columns("Balance").DefaultCellStyle.ForeColor = ModernTheme.Danger

        Me.Controls.Add(dgvInvoices)

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
        AddHandler btnRefresh.Click, Sub() LoadInvoices()
        Me.Controls.Add(btnRefresh)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Close")
        btnClose.Location = New Point(710, 590)
        btnClose.Width = 130
        btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)

        AddHandler Me.Shown, Sub(s, e) LoadInvoices()
        LoadInvoices()
    End Sub

    Private Sub LoadInvoices()
        dgvInvoices.Rows.Clear()
        Dim totalOutstanding As Decimal = 0

        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Const sql As String = "SELECT si.InvoiceID, c.CustomerName, si.InvoiceDate, si.TotalAmount, si.AmountPaid, si.Status, " &
                                      "DATEDIFF(DAY, si.InvoiceDate, GETDATE()) AS DaysSinceInvoice " &
                                      "FROM mn.SalesInvoice si " &
                                      "JOIN mn.Customer c ON c.CustomerID = si.CustomerID " &
                                      "WHERE si.Status IN ('UNPAID', 'PARTIAL') " &
                                      "ORDER BY si.InvoiceDate ASC"
                
                Using cmd As New SqlCommand(sql, conn)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim invoiceID = reader.GetInt32(0)
                            Dim customerName = reader.GetString(1)
                            Dim invoiceDate = reader.GetDateTime(2).ToString("yyyy-MM-dd")
                            Dim totalAmount = reader.GetDecimal(3)
                            Dim amountPaid = reader.GetDecimal(4)
                            Dim status = reader.GetString(5)
                            Dim daysSinceInvoice = reader.GetInt32(6)
                            Dim balance = totalAmount - amountPaid

                            totalOutstanding += balance

                            Dim rowIdx = dgvInvoices.Rows.Add(invoiceID, customerName, invoiceDate, totalAmount, amountPaid, balance, status, daysSinceInvoice)
                            
                            If daysSinceInvoice > 30 Then
                                dgvInvoices.Rows(rowIdx).DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235)
                            End If
                        End While
                    End Using
                End Using
            End Using

            lblTotalOutstanding.Text = $"Total outstanding: {totalOutstanding:N2} ETB"
        Catch ex As Exception
            AppLogger.Error("Failed to load outstanding receivables", ex)
            MessageBox.Show("Unable to load receivables: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
