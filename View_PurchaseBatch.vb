Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Collections.Generic
Imports System.Text
Imports System.IO

Public Class View_PurchaseBatch

    Public Structure PurchaseBatch
        Public BatchID As Integer
        Public PartNumber As String
        Public SupplierName As String
        Public QuantityPurchased As Integer
        Public QuantityRemaining As Integer
        Public UnitCost As Decimal
        Public DatePurchased As DateTime
    End Structure

    Private batchesArray() As PurchaseBatch
    Private filteredArray() As PurchaseBatch

    ' UI Controls
    Private WithEvents dgvBatches As DataGridView
    Private txtSearch As TextBox
    Private btnSearch As Button
    Private btnClear As Button
    Private btnExport As Button
    Private lblStats As Label


    Private totalBatches As Integer = 0
    Private totalPurchased As Integer = 0
    Private totalRemaining As Integer = 0
    Private totalValue As Decimal = 0

    Private Sub View_PurchaseBatch_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupForm()
        LoadPurchaseBatches()
    End Sub
    Private Sub SetupForm()
        Me.Text = "Purchase Batch Tracking (FIFO)"
        Me.Size = New Size(1100, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(244, 246, 248)

        ' Title
        Dim lblTitle As New Label With {
            .Text = "PURCHASE BATCHES - FIFO TRACKING",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.FromArgb(21, 101, 192),
            .Location = New Point(20, 15),
            .Size = New Size(600, 35)
        }
        Me.Controls.Add(lblTitle)

        ' Search Panel
        Dim pnlSearch As New Panel With {
            .Location = New Point(20, 60),
            .Size = New Size(1045, 60),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.None
        }

        Dim lblSearch As New Label With {
            .Text = "Part Number:",
            .Location = New Point(15, 20),
            .Size = New Size(100, 20),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
        pnlSearch.Controls.Add(lblSearch)

        txtSearch = New TextBox With {
            .Location = New Point(120, 18),
            .Size = New Size(300, 25),
            .Font = New Font("Segoe UI", 10)
        }
        AddHandler txtSearch.KeyPress, AddressOf txtSearch_KeyPress
        pnlSearch.Controls.Add(txtSearch)

        btnSearch = New Button With {
            .Text = "Search",
            .Location = New Point(430, 15),
            .Size = New Size(120, 32),
            .BackColor = Color.FromArgb(59, 130, 246),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlSearch.Controls.Add(btnSearch)

        btnClear = New Button With {
            .Text = "Reset",
            .Location = New Point(560, 15),
            .Size = New Size(100, 32),
            .BackColor = Color.FromArgb(108, 117, 125),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnClear.Click, AddressOf btnClear_Click
        pnlSearch.Controls.Add(btnClear)
        Me.Controls.Add(pnlSearch)

        ' THE TABLE (DataGridView) - FULL FIT
        dgvBatches = New DataGridView With {
            .Location = New Point(20, 140),
            .Size = New Size(1045, 350),
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

        dgvBatches.Columns.Add("BatchID", "Batch ID")
        dgvBatches.Columns.Add("PartNumber", "Part Number")
        dgvBatches.Columns.Add("SupplierName", "Supplier")
        dgvBatches.Columns.Add("QuantityPurchased", "Purchased")
        dgvBatches.Columns.Add("QuantityRemaining", "Remaining")
        dgvBatches.Columns.Add("UnitCost", "Unit Cost")
        dgvBatches.Columns.Add("DatePurchased", "Date")

        ' Adjust column weight
        dgvBatches.Columns("BatchID").FillWeight = 50
        dgvBatches.Columns("PartNumber").FillWeight = 100
        dgvBatches.Columns("SupplierName").FillWeight = 150
        dgvBatches.Columns("QuantityPurchased").FillWeight = 70
        dgvBatches.Columns("QuantityRemaining").FillWeight = 70
        dgvBatches.Columns("UnitCost").FillWeight = 80
        dgvBatches.Columns("DatePurchased").FillWeight = 90

        ' Alignment and Formatting
        dgvBatches.Columns("UnitCost").DefaultCellStyle.Format = "N2"
        dgvBatches.Columns("QuantityPurchased").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvBatches.Columns("QuantityRemaining").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvBatches.Columns("UnitCost").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        Me.Controls.Add(dgvBatches)

        ' Statistics Label
        lblStats = New Label With {
            .Location = New Point(20, 500),
            .Size = New Size(1045, 30),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 40, 40),
            .BackColor = Color.Transparent
        }
        Me.Controls.Add(lblStats)

        ' Bottom Action Panel
        Dim pnlActions As New Panel With {.Location = New Point(20, 540), .Size = New Size(1045, 50)}

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


    Private Sub LoadPurchaseBatches(Optional ByVal partFilter As String = "")
        batchesArray = GetPurchaseBatchesFromDB(partFilter)
        filteredArray = batchesArray
        DisplayBatches(filteredArray)
        UpdateStatistics()
    End Sub

    Private Function GetPurchaseBatchesFromDB(Optional ByVal partFilter As String = "") As PurchaseBatch()
        Dim list As New List(Of PurchaseBatch)
        Try
            DBModule.OpenConnection()
            ' Fixed multi-line string for older VB.NET compilers
            Dim query As String = "SELECT pb.BatchID, pb.PartNumber, ISNULL(s.SupplierName, 'N/A') AS SupplierName, " &
                                 "pb.QuantityPurchased, pb.QuantityRemaining, pb.UnitCost, pb.PurchaseDate " &
                                 "FROM mn.PurchaseBatch pb " &
                                 "LEFT JOIN mn.Supplier s ON pb.SupplierID = s.SupplierID"

            If Not String.IsNullOrEmpty(partFilter) Then
                query &= " WHERE pb.PartNumber LIKE @p"
            End If
            query &= " ORDER BY pb.PurchaseDate DESC"

            Using cmd As New SqlCommand(query, DBModule.Conn)
                If Not String.IsNullOrEmpty(partFilter) Then cmd.Parameters.AddWithValue("@p", "%" & partFilter & "%")
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        list.Add(New PurchaseBatch With {
                            .BatchID = CInt(reader("BatchID")),
                            .PartNumber = reader("PartNumber").ToString(),
                            .SupplierName = reader("SupplierName").ToString(),
                            .QuantityPurchased = CInt(reader("QuantityPurchased")),
                            .QuantityRemaining = CInt(reader("QuantityRemaining")),
                            .UnitCost = CDec(reader("UnitCost")),
                            .DatePurchased = CDate(reader("PurchaseDate"))
                        })
                    End While
                End Using
            End Using
            DBModule.CloseConnection()
        Catch ex As Exception
            MessageBox.Show("DB Error: " & ex.Message)
        End Try
        Return list.ToArray()
    End Function

    Private Sub DisplayBatches(batches As PurchaseBatch())
        dgvBatches.Rows.Clear()
        For Each b In batches
            Dim rowIndex As Integer = dgvBatches.Rows.Add(b.BatchID, b.PartNumber, b.SupplierName, b.QuantityPurchased, b.QuantityRemaining, b.UnitCost, b.DatePurchased.ToString("yyyy-MM-dd"))

            ' Visual cues for remaining stock
            If b.QuantityRemaining = 0 Then
                dgvBatches.Rows(rowIndex).DefaultCellStyle.ForeColor = Color.Gray
            ElseIf b.QuantityRemaining < (b.QuantityPurchased * 0.2) Then
                dgvBatches.Rows(rowIndex).Cells("QuantityRemaining").Style.BackColor = Color.MistyRose
            End If
        Next
    End Sub

    Private Sub UpdateStatistics()
        totalBatches = filteredArray.Length
        totalPurchased = 0
        totalRemaining = 0
        totalValue = 0

        For Each batch In filteredArray
            totalPurchased += batch.QuantityPurchased
            totalRemaining += batch.QuantityRemaining
            totalValue += batch.QuantityRemaining * batch.UnitCost
        Next

        lblStats.Text = String.Format("Batches: {0} | Purchased: {1:N0} | Remaining: {2:N0} | Remaining value: {3:N2}", totalBatches, totalPurchased, totalRemaining, totalValue)
    End Sub

    ' ============ EVENTS ============
    Private Sub btnSearch_Click(sender As Object, e As EventArgs)
        Dim searchText As String = txtSearch.Text.Trim()
        ' Use LINQ for fast local filtering or reload from DB
        LoadPurchaseBatches(searchText)
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs)
        txtSearch.Clear()
        LoadPurchaseBatches("")
    End Sub

    Private Sub txtSearch_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            btnSearch_Click(Nothing, Nothing)
            e.Handled = True
        End If
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs)
        If filteredArray Is Nothing OrElse filteredArray.Length = 0 Then Return

        Using sfd As New SaveFileDialog()
            sfd.Filter = "CSV Files (*.csv)|*.csv"
            sfd.FileName = "PurchaseBatches_" & DateTime.Now.ToString("yyyyMMdd") & ".csv"

            If sfd.ShowDialog() = DialogResult.OK Then
                Dim sb As New StringBuilder()
                sb.AppendLine("BatchID,PartNumber,Supplier,Purchased,Remaining,UnitCost,Date")

                For Each b In filteredArray
                    sb.AppendLine(String.Format("{0},{1},{2},{3},{4},{5},{6}",
                        b.BatchID, b.PartNumber, b.SupplierName, b.QuantityPurchased, b.QuantityRemaining, b.UnitCost, b.DatePurchased.ToString("yyyy-MM-dd")))
                Next

                File.WriteAllText(sfd.FileName, sb.ToString())
                MessageBox.Show("Export Successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub
End Class