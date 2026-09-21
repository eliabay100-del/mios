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
        SetupForm()
        LoadSoldItems()
    End Sub

    Private Sub SetupForm()
        Me.Text = "Sales & Issues Tracking"
        Me.Size = New Size(1150, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(244, 246, 248)

        ' Title
        Dim lblTitle As New Label With {
            .Text = "ISSUED PARTS / SOLD ITEMS",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.FromArgb(21, 101, 192),
            .Location = New Point(20, 15),
            .Size = New Size(500, 35)
        }
        Me.Controls.Add(lblTitle)

        ' Filter Panel
        Dim pnlFilter As New Panel With {
            .Location = New Point(20, 60),
            .Size = New Size(1095, 70),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.None
        }

        ' Part Number Search
        Dim lblPart As New Label With {.Text = "Part Number:", .Location = New Point(15, 25), .Size = New Size(90, 20), .Font = New Font("Segoe UI", 9, FontStyle.Bold)}
        txtPartNumber = New TextBox With {.Location = New Point(105, 22), .Size = New Size(140, 25)}
        pnlFilter.Controls.Add(lblPart)
        pnlFilter.Controls.Add(txtPartNumber)

        ' Phone Search
        Dim lblPhone As New Label With {.Text = "Phone:", .Location = New Point(260, 25), .Size = New Size(50, 20), .Font = New Font("Segoe UI", 9, FontStyle.Bold)}
        txtPhoneNumber = New TextBox With {.Location = New Point(315, 22), .Size = New Size(140, 25)}
        pnlFilter.Controls.Add(lblPhone)
        pnlFilter.Controls.Add(txtPhoneNumber)

        ' Date Picker
        Dim lblDate As New Label With {.Text = "Date:", .Location = New Point(470, 25), .Size = New Size(40, 20), .Font = New Font("Segoe UI", 9, FontStyle.Bold)}
        dtpDate = New DateTimePicker With {
            .Location = New Point(515, 22),
            .Size = New Size(130, 25),
            .Format = DateTimePickerFormat.Short,
            .ShowCheckBox = True,
            .Checked = False
        }
        pnlFilter.Controls.Add(lblDate)
        pnlFilter.Controls.Add(dtpDate)

        ' Search Buttons
        btnSearch = New Button With {
            .Text = "Search", .Location = New Point(660, 18), .Size = New Size(100, 32),
            .BackColor = Color.FromArgb(59, 130, 246), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlFilter.Controls.Add(btnSearch)

        btnClear = New Button With {
            .Text = "Reset", .Location = New Point(770, 18), .Size = New Size(90, 32),
            .BackColor = Color.FromArgb(108, 117, 125), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnClear.Click, AddressOf btnClear_Click
        pnlFilter.Controls.Add(btnClear)

        Me.Controls.Add(pnlFilter)

        ' THE TABLE (DataGridView) - FULL FIT
        dgvSoldItems = New DataGridView With {
            .Location = New Point(20, 150),
            .Size = New Size(1095, 340),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle With {.BackColor = Color.FromArgb(245, 247, 250)},
            .Font = New Font("Segoe UI", 10)
        }

        dgvSoldItems.Columns.Add("IssueDate", "Date Issued")
        dgvSoldItems.Columns.Add("PartNumber", "Part Number")
        dgvSoldItems.Columns.Add("QuantityIssued", "Qty")
        dgvSoldItems.Columns.Add("SellingPrice", "Price (Birr)")
        dgvSoldItems.Columns.Add("CustomerName", "Customer")
        dgvSoldItems.Columns.Add("PhoneNumber", "Phone")
        dgvSoldItems.Columns.Add("Remarks", "Remarks")

        ' Adjust column weight for "Full Fit"
        dgvSoldItems.Columns("IssueDate").FillWeight = 100
        dgvSoldItems.Columns("PartNumber").FillWeight = 100
        dgvSoldItems.Columns("QuantityIssued").FillWeight = 50
        dgvSoldItems.Columns("SellingPrice").FillWeight = 90
        dgvSoldItems.Columns("CustomerName").FillWeight = 140
        dgvSoldItems.Columns("PhoneNumber").FillWeight = 100
        dgvSoldItems.Columns("Remarks").FillWeight = 120

        ' Formatting
        dgvSoldItems.Columns("SellingPrice").DefaultCellStyle.Format = "N2"
        dgvSoldItems.Columns("QuantityIssued").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvSoldItems.Columns("SellingPrice").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        AddHandler dgvSoldItems.CellFormatting, AddressOf dgvSoldItems_CellFormatting
        Me.Controls.Add(dgvSoldItems)

        ' Statistics Label
        lblStats = New Label With {
            .Location = New Point(20, 500),
            .Size = New Size(1095, 30),
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 40, 40)
        }
        Me.Controls.Add(lblStats)

        ' Action Panel
        Dim pnlActions As New Panel With {.Location = New Point(20, 540), .Size = New Size(1095, 50)}

        btnExport = New Button With {
            .Text = "Export to CSV", .Location = New Point(0, 5), .Size = New Size(130, 35),
            .BackColor = Color.FromArgb(34, 197, 94), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnExport.Click, AddressOf btnExport_Click
        pnlActions.Controls.Add(btnExport)

        Dim btnClose As New Button With {
            .Text = "Back to Menu", .Location = New Point(140, 5), .Size = New Size(120, 35),
            .BackColor = Color.White, .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnClose.Click, Sub() Me.Close()
        pnlActions.Controls.Add(btnClose)

        Me.Controls.Add(pnlActions)
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
            DBModule.OpenConnection()
            Dim query As String = "SELECT ii.IssueDate, ii.PartNumber, ii.QuantityIssued, sp.UnitPrice, " &
                                 "c.CustomerName, c.Phone FROM mn.IssuedItem ii " &
                                 "JOIN mn.SpareParts sp ON ii.PartNumber = sp.PartNumber " &
                                 "JOIN mn.Customer c ON ii.CustomerID = c.CustomerID"

            Dim whereClauses As New List(Of String)
            If Not String.IsNullOrEmpty(partFilter) Then whereClauses.Add("ii.PartNumber LIKE @p")
            If Not String.IsNullOrEmpty(phoneFilter) Then whereClauses.Add("c.Phone LIKE @phone")
            If dateFilter.HasValue Then whereClauses.Add("CAST(ii.IssueDate AS DATE) = @date")

            If whereClauses.Count > 0 Then query &= " WHERE " & String.Join(" AND ", whereClauses.ToArray())
            query &= " ORDER BY ii.IssueDate DESC"

            Using cmd As New SqlCommand(query, DBModule.Conn)
                If Not String.IsNullOrEmpty(partFilter) Then cmd.Parameters.AddWithValue("@p", "%" & partFilter & "%")
                If Not String.IsNullOrEmpty(phoneFilter) Then cmd.Parameters.AddWithValue("@phone", "%" & phoneFilter & "%")
                If dateFilter.HasValue Then cmd.Parameters.AddWithValue("@date", dateFilter.Value)

                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        list.Add(New SoldItem With {
                            .IssueDate = CDate(reader("IssueDate")),
                            .PartNumber = reader("PartNumber").ToString(),
                            .QuantityIssued = CInt(reader("QuantityIssued")),
                            .SellingPrice = CDec(reader("UnitPrice")),
                            .CustomerName = reader("CustomerName").ToString(),
                            .PhoneNumber = If(reader("Phone") Is DBNull.Value, "N/A", reader("Phone").ToString()),
                            .Remarks = "Sold"
                        })
                    End While
                End Using
            End Using
            DBModule.CloseConnection()
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message)
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
                dgvSoldItems.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.LightGreen
            End If
        End If
    End Sub

    Private Sub btnSearch_Click(sender As Object, e As EventArgs)
        LoadSoldItems()
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
                Dim sb As New StringBuilder()
                sb.AppendLine("Date,PartNumber,Quantity,Price,Customer,Phone")
                For Each i In filteredArray
                    sb.AppendLine(String.Format("{0},{1},{2},{3},{4},{5}", i.IssueDate.ToString("yyyy-MM-dd HH:mm"), i.PartNumber, i.QuantityIssued, i.SellingPrice, i.CustomerName, i.PhoneNumber))
                Next
                File.WriteAllText(sfd.FileName, sb.ToString())
                MessageBox.Show("Export Successful!")
            End If
        End Using
    End Sub
End Class