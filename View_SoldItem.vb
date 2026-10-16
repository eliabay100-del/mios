Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Collections.Generic
Imports System.Text
Imports System.IO

Public Class View_SoldItem

    Public Structure SoldItem
        Public IssueDate As DateTime
        Public PartNumber As String
        Public QuantityIssued As Integer
        Public SellingPrice As Decimal
        Public CustomerName As String
        Public PhoneNumber As String
        Public Remarks As String
    End Structure

    Private soldItemsArray() As SoldItem
    Private filteredArray() As SoldItem

    ' Form Controls
    Private WithEvents dgvSoldItems As DataGridView
    Private txtPartNumber As TextBox
    Private txtPhoneNumber As TextBox
    Private dtpDate As DateTimePicker
    Private btnSearch As Button
    Private btnClear As Button
    Private btnExport As Button
    Private lblStats As Label

    ' Statistics Counters
    Private totalItems As Integer = 0
    Private totalQuantity As Integer = 0
    Private totalRevenue As Decimal = 0

    Private Sub View_SoldItem_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ModernTheme.ApplyWindowTheme(Me)
        SetupForm()
        LoadSoldItems()
    End Sub

    Private Sub SetupForm()
        Me.Text = "Sales & Issues Tracking"
        Me.Size = New Size(1080, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        ModernTheme.EnableVerticalScroll(Me, 700)

        ' Title
        Dim lblTitle As New Label With {
            .Text = "Sales & issues tracking",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(22, 18),
            .Size = New Size(400, 34)
        }
        Me.Controls.Add(lblTitle)

        ' Filter bar 1 - part number search + actions
        Dim pnlFilter As New Panel With {
            .Location = New Point(22, 64),
            .Size = New Size(1010, 52),
            .BackColor = Color.White
        }

        Dim lblPart As New Label With {
            .Text = "Part Number:",
            .Location = New Point(12, 8),
            .Size = New Size(100, 30),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        pnlFilter.Controls.Add(lblPart)

        txtPartNumber = ModernTheme.MakeTextBox()
        txtPartNumber.Location = New Point(120, 8)
        txtPartNumber.Width = 300
        AddHandler txtPartNumber.KeyPress, AddressOf SearchField_KeyPress
        pnlFilter.Controls.Add(txtPartNumber)

        btnSearch = ModernTheme.MakePrimaryButton("Search")
        btnSearch.Location = New Point(430, 8)
        btnSearch.Width = 120
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlFilter.Controls.Add(btnSearch)

        btnClear = ModernTheme.MakeSecondaryButton("Clear all")
        btnClear.Location = New Point(560, 8)
        btnClear.Width = 120
        btnClear.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnClear.Click, AddressOf btnClear_Click
        pnlFilter.Controls.Add(btnClear)
        Me.Controls.Add(pnlFilter)

        ' Filter bar 2 - phone + date
        Dim pnlFilter2 As New Panel With {
            .Location = New Point(22, 124),
            .Size = New Size(1010, 52),
            .BackColor = Color.White
        }

        Dim lblPhone As New Label With {
            .Text = "Phone:",
            .Location = New Point(12, 8),
            .Size = New Size(60, 30),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        pnlFilter2.Controls.Add(lblPhone)

        txtPhoneNumber = ModernTheme.MakeTextBox()
        txtPhoneNumber.Location = New Point(80, 8)
        txtPhoneNumber.Width = 260
        AddHandler txtPhoneNumber.KeyPress, AddressOf SearchField_KeyPress
        pnlFilter2.Controls.Add(txtPhoneNumber)

        Dim lblDate As New Label With {
            .Text = "Date:",
            .Location = New Point(350, 8),
            .Size = New Size(50, 30),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        pnlFilter2.Controls.Add(lblDate)

        dtpDate = New DateTimePicker With {
            .Location = New Point(408, 9),
            .Size = New Size(170, 34),
            .Format = DateTimePickerFormat.Short,
            .ShowCheckBox = True,
            .Checked = False,
            .Font = New Font("Segoe UI", 10)
        }
        pnlFilter2.Controls.Add(dtpDate)
        Me.Controls.Add(pnlFilter2)

        ' THE TABLE (DataGridView) - FULL FIT
        dgvSoldItems = New DataGridView With {
            .Location = New Point(22, 186),
            .Size = New Size(1010, 360),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .AllowUserToResizeColumns = False,
            .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle With {.BackColor = ModernTheme.SurfaceAlt},
            .Font = New Font("Segoe UI", 10)
        }
        ModernTheme.StyleGrid(dgvSoldItems)

        dgvSoldItems.Columns.Add("IssueDate", "Date Issued")
        dgvSoldItems.Columns.Add("PartNumber", "Part Number")
        dgvSoldItems.Columns.Add("QuantityIssued", "Qty")
        dgvSoldItems.Columns.Add("SellingPrice", "Price (Birr)")
        dgvSoldItems.Columns.Add("CustomerName", "Customer")
        dgvSoldItems.Columns.Add("PhoneNumber", "Phone")
        dgvSoldItems.Columns.Add("Remarks", "Remarks")

        ' Fixed column widths (rigid, non-resizable)
        dgvSoldItems.Columns("IssueDate").Width = 144
        dgvSoldItems.Columns("PartNumber").Width = 144
        dgvSoldItems.Columns("QuantityIssued").Width = 72
        dgvSoldItems.Columns("SellingPrice").Width = 130
        dgvSoldItems.Columns("CustomerName").Width = 202
        dgvSoldItems.Columns("PhoneNumber").Width = 144
        dgvSoldItems.Columns("Remarks").Width = 174

        ' Formatting
        dgvSoldItems.Columns("SellingPrice").DefaultCellStyle.Format = "N2"
        dgvSoldItems.Columns("QuantityIssued").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvSoldItems.Columns("SellingPrice").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        AddHandler dgvSoldItems.CellFormatting, AddressOf dgvSoldItems_CellFormatting
        Me.Controls.Add(dgvSoldItems)

        ' Statistics Label
        lblStats = New Label With {
            .Location = New Point(22, 552),
            .Size = New Size(1010, 30),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        Me.Controls.Add(lblStats)

        ' Bottom action row
        btnExport = ModernTheme.MakePrimaryButton("Export to CSV")
        btnExport.Location = New Point(870, 590)
        btnExport.Width = 160
        AddHandler btnExport.Click, AddressOf btnExport_Click
        Me.Controls.Add(btnExport)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Back to dashboard")
        btnClose.Location = New Point(22, 590)
        btnClose.Width = 180
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)
    End Sub


    Private Sub LoadSoldItems()
        Dim partFilter As String = txtPartNumber.Text.Trim()
        Dim phoneFilter As String = txtPhoneNumber.Text.Trim()
        Dim dateFilter As Date? = If(dtpDate.Checked, dtpDate.Value.Date, CType(Nothing, Date?))

        soldItemsArray = GetSoldItemsFromDB(partFilter, phoneFilter, dateFilter)
        filteredArray = soldItemsArray
        DisplaySoldItems(filteredArray)
        UpdateStatistics()
    End Sub

    Private Function GetSoldItemsFromDB(partFilter As String, phoneFilter As String, dateFilter As Date?) As SoldItem()
        Dim list As New List(Of SoldItem)
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                ' Report from the sales invoice lines so the price shown is the one
                ' charged at the time of sale, not the part's current price.
                Dim query As String = "SELECT si.InvoiceDate, sil.PartNumber, sil.Quantity, sil.UnitPrice, " &
                                     "c.CustomerName, c.Phone FROM mn.SalesInvoiceLine sil " &
                                     "JOIN mn.SalesInvoice si ON si.InvoiceID = sil.InvoiceID " &
                                     "JOIN mn.Customer c ON c.CustomerID = si.CustomerID"

                Dim whereClauses As New List(Of String)
                If Not String.IsNullOrEmpty(partFilter) Then whereClauses.Add("sil.PartNumber LIKE @p")
                If Not String.IsNullOrEmpty(phoneFilter) Then whereClauses.Add("c.Phone LIKE @phone")
                If dateFilter.HasValue Then whereClauses.Add("CAST(si.InvoiceDate AS DATE) = @date")

                If whereClauses.Count > 0 Then query &= " WHERE " & String.Join(" AND ", whereClauses.ToArray())
                query &= " ORDER BY si.InvoiceDate DESC, sil.InvoiceLineID DESC"

                Using cmd As New SqlCommand(query, conn)
                    If Not String.IsNullOrEmpty(partFilter) Then cmd.Parameters.AddWithValue("@p", "%" & partFilter & "%")
                    If Not String.IsNullOrEmpty(phoneFilter) Then cmd.Parameters.AddWithValue("@phone", "%" & phoneFilter & "%")
                    If dateFilter.HasValue Then cmd.Parameters.AddWithValue("@date", dateFilter.Value)

                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(New SoldItem With {
                                .IssueDate = CDate(reader("InvoiceDate")),
                                .PartNumber = reader("PartNumber").ToString(),
                                .QuantityIssued = CInt(reader("Quantity")),
                                .SellingPrice = CDec(reader("UnitPrice")),
                                .CustomerName = reader("CustomerName").ToString(),
                                .PhoneNumber = If(reader("Phone") Is DBNull.Value, "N/A", reader("Phone").ToString()),
                                .Remarks = "Sold"
                            })
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load sold items", ex)
            MessageBox.Show("Unable to load sold items: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Return list.ToArray()
    End Function

    Private Sub DisplaySoldItems(items As SoldItem())
        dgvSoldItems.Rows.Clear()
        For Each i In items
            dgvSoldItems.Rows.Add(i.IssueDate.ToString("yyyy-MM-dd HH:mm"), i.PartNumber, i.QuantityIssued, i.SellingPrice, i.CustomerName, i.PhoneNumber, i.Remarks)
        Next
    End Sub

    Private Sub UpdateStatistics()
        totalItems = filteredArray.Length
        totalQuantity = filteredArray.Sum(Function(item) item.QuantityIssued)
        totalRevenue = filteredArray.Sum(Function(item) item.QuantityIssued * item.SellingPrice)

        lblStats.Text = "Transactions: " & totalItems & " | Total quantity: " & totalQuantity.ToString("N0") & " | Revenue: " & totalRevenue.ToString("N2")
    End Sub

    ' ============ EVENTS ============
    Private Sub dgvSoldItems_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex >= 0 AndAlso e.ColumnIndex = 0 Then
            Dim issueDate As DateTime = CDate(dgvSoldItems.Rows(e.RowIndex).Cells("IssueDate").Value)
            If issueDate.Date = DateTime.Now.Date Then
                dgvSoldItems.Rows(e.RowIndex).DefaultCellStyle.SelectionBackColor = Color.LightGreen
            End If
        End If
    End Sub

    Private Sub btnSearch_Click(sender As Object, e As EventArgs)
        LoadSoldItems()
    End Sub

    Private Sub SearchField_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            LoadSoldItems()
            e.Handled = True
        End If
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs)
        txtPartNumber.Clear()
        txtPhoneNumber.Clear()
        dtpDate.Checked = False
        LoadSoldItems()
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs)
        If filteredArray Is Nothing OrElse filteredArray.Length = 0 Then Return
        Using sfd As New SaveFileDialog()
            sfd.Filter = "CSV Files (*.csv)|*.csv"
            sfd.FileName = "SalesReport_" & DateTime.Now.ToString("yyyyMMdd") & ".csv"
            If sfd.ShowDialog() = DialogResult.OK Then
                Try
                    Dim sb As New StringBuilder()
                    sb.AppendLine("Date,PartNumber,Quantity,Price,Customer,Phone")
                    For Each i In filteredArray
                        sb.AppendLine(String.Join(",", {CsvField(i.IssueDate.ToString("yyyy-MM-dd HH:mm")), CsvField(i.PartNumber),
                            CsvField(i.QuantityIssued), CsvField(i.SellingPrice), CsvField(i.CustomerName), CsvField(i.PhoneNumber)}))
                    Next
                    File.WriteAllText(sfd.FileName, sb.ToString())
                    MessageBox.Show("Export Successful!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    AppLogger.Error("Failed to export sold items", ex)
                    MessageBox.Show("Export failed: " & ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    ''' <summary>Quotes a CSV field when it contains a comma, quote or newline.</summary>
    Private Shared Function CsvField(value As Object) As String
        Dim text As String = If(value Is Nothing, "", value.ToString())
        If text.Contains(","c) OrElse text.Contains(""""c) OrElse text.Contains(vbCr) OrElse text.Contains(vbLf) Then
            Return """" & text.Replace("""", """""") & """"
        End If
        Return text
    End Function
End Class
