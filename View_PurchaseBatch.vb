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
        ModernTheme.ApplyWindowTheme(Me)
        SetupForm()
        LoadPurchaseBatches()
    End Sub
    Private Sub SetupForm()
        Me.Text = "Purchase Batch Tracking (FIFO)"
        Me.Size = New Size(1080, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        ModernTheme.EnableVerticalScroll(Me, 700)

        Dim lblTitle As New Label With {
            .Text = "Purchase batches",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(22, 18),
            .Size = New Size(400, 34)
        }
        Me.Controls.Add(lblTitle)

        Dim pnlSearch As New Panel With {
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
        pnlSearch.Controls.Add(lblPart)

        txtSearch = ModernTheme.MakeTextBox()
        txtSearch.Location = New Point(120, 8)
        txtSearch.Width = 300
        AddHandler txtSearch.KeyPress, AddressOf txtSearch_KeyPress
        pnlSearch.Controls.Add(txtSearch)

        btnSearch = ModernTheme.MakePrimaryButton("Search")
        btnSearch.Location = New Point(430, 8)
        btnSearch.Width = 120
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlSearch.Controls.Add(btnSearch)

        btnClear = ModernTheme.MakeSecondaryButton("Clear all")
        btnClear.Location = New Point(560, 8)
        btnClear.Width = 120
        btnClear.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnClear.Click, AddressOf btnClear_Click
        pnlSearch.Controls.Add(btnClear)
        Me.Controls.Add(pnlSearch)

        dgvBatches = New DataGridView With {
            .Location = New Point(22, 130),
            .Size = New Size(1010, 400),
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
        ModernTheme.StyleGrid(dgvBatches)

        dgvBatches.Columns.Add("BatchID", "Batch ID")
        dgvBatches.Columns.Add("PartNumber", "Part Number")
        dgvBatches.Columns.Add("SupplierName", "Supplier")
        dgvBatches.Columns.Add("QuantityPurchased", "Purchased")
        dgvBatches.Columns.Add("QuantityRemaining", "Remaining")
        dgvBatches.Columns.Add("UnitCost", "Unit Cost")
        dgvBatches.Columns.Add("DatePurchased", "Date")

        ' Fixed column widths (rigid, non-resizable)
        dgvBatches.Columns("BatchID").Width = 83
        dgvBatches.Columns("PartNumber").Width = 166
        dgvBatches.Columns("SupplierName").Width = 248
        dgvBatches.Columns("QuantityPurchased").Width = 116
        dgvBatches.Columns("QuantityRemaining").Width = 116
        dgvBatches.Columns("UnitCost").Width = 132
        dgvBatches.Columns("DatePurchased").Width = 149

        ' Alignment and Formatting
        dgvBatches.Columns("UnitCost").DefaultCellStyle.Format = "N2"
        dgvBatches.Columns("QuantityPurchased").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvBatches.Columns("QuantityRemaining").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvBatches.Columns("UnitCost").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        Me.Controls.Add(dgvBatches)

        lblStats = New Label With {
            .Location = New Point(22, 538),
            .Size = New Size(1010, 30),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        Me.Controls.Add(lblStats)

        btnExport = ModernTheme.MakePrimaryButton("Export to CSV")
        btnExport.Location = New Point(870, 570)
        btnExport.Width = 160
        AddHandler btnExport.Click, AddressOf btnExport_Click
        Me.Controls.Add(btnExport)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Back to dashboard")
        btnClose.Location = New Point(22, 570)
        btnClose.Width = 180
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)
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
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Dim query As String = "SELECT pb.BatchID, pb.PartNumber, ISNULL(s.SupplierName, 'N/A') AS SupplierName, " &
                                     "pb.QuantityPurchased, pb.QuantityRemaining, pb.UnitCost, pb.PurchaseDate " &
                                     "FROM mn.PurchaseBatch pb " &
                                     "LEFT JOIN mn.Supplier s ON pb.SupplierID = s.SupplierID"

                If Not String.IsNullOrEmpty(partFilter) Then
                    query &= " WHERE pb.PartNumber LIKE @p"
                End If
                query &= " ORDER BY pb.PurchaseDate DESC, pb.BatchID DESC"

                Using cmd As New SqlCommand(query, conn)
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
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load purchase batches", ex)
            MessageBox.Show("Unable to load purchase batches: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                dgvBatches.Rows(rowIndex).Cells("QuantityRemaining").Style.SelectionBackColor = Color.MistyRose
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
                Try
                    Dim sb As New StringBuilder()
                    sb.AppendLine("BatchID,PartNumber,Supplier,Purchased,Remaining,UnitCost,Date")

                    For Each b In filteredArray
                        sb.AppendLine(String.Join(",", {CsvField(b.BatchID), CsvField(b.PartNumber), CsvField(b.SupplierName),
                            CsvField(b.QuantityPurchased), CsvField(b.QuantityRemaining), CsvField(b.UnitCost), CsvField(b.DatePurchased.ToString("yyyy-MM-dd"))}))
                    Next

                    File.WriteAllText(sfd.FileName, sb.ToString())
                    MessageBox.Show("Export Successful!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    AppLogger.Error("Failed to export purchase batches", ex)
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
