Imports System.Drawing
Imports System.Windows.Forms
Imports System.Data.SqlClient
Imports System.Text

Public Class Supplier
    Inherits Form

    Public Structure SupplierInfo
        Public SupplierID As Integer
        Public SupplierName As String
        Public Phone As String
        Public Address As String
    End Structure

    Public Structure PurchaseSummary
        Public PurchaseDate As Date
        Public TotalQuantityPurchased As Integer
        Public TotalQuantityRemaining As Integer
        Public TotalCost As Decimal
    End Structure

    Private suppliersArray() As SupplierInfo
    Private filteredArray() As SupplierInfo
    Private purchaseSummaryArray() As PurchaseSummary
    Private viewMode As String = "LIST"
    Private selectedSupplierID As Integer = -1
    Private selectedSupplierName As String = ""

    Private WithEvents dgvSuppliers As DataGridView
    Private WithEvents dgvPurchases As DataGridView
    Private WithEvents txtSearch As TextBox
    Private WithEvents btnSearch As Button
    Private WithEvents btnClear As Button
    Private WithEvents btnExport As Button
    Private WithEvents btnViewRecords As Button
    Private WithEvents btnBackToSuppliers As Button
    Private lblTitle As Label
    Private lblSupplierTitle As Label
    Private lblSearchLabel As Label

    Public Sub New()
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Supplier Management"
        Me.Size = New Size(1120, 680)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        ModernTheme.EnableVerticalScroll(Me, 760)
        InitializeSupplierForm()
    End Sub

    Private Sub InitializeSupplierForm()
        lblTitle = New Label With {
            .Text = "Supplier information",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(24, 18),
            .Size = New Size(320, 34)
        }
        Me.Controls.Add(lblTitle)

        lblSupplierTitle = New Label With {
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(24, 62),
            .Size = New Size(800, 26),
            .Visible = False
        }
        Me.Controls.Add(lblSupplierTitle)

        lblSearchLabel = New Label With {
            .Text = "Search supplier",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText,
            .Location = New Point(24, 104),
            .Size = New Size(140, 24)
        }
        Me.Controls.Add(lblSearchLabel)

        Dim pnlSearch As New Panel With {.Location = New Point(170, 98), .Size = New Size(920, 42), .BackColor = Color.White}
        txtSearch = ModernTheme.MakeTextBox()
        txtSearch.Location = New Point(12, 8)
        txtSearch.Width = 360
        btnSearch = ModernTheme.MakePrimaryButton("Search")
        btnSearch.Location = New Point(390, 6)
        btnSearch.Width = 110
        btnClear = ModernTheme.MakeSecondaryButton("Clear")
        btnClear.Location = New Point(510, 6)
        btnClear.Width = 110
        btnClear.BackColor = ModernTheme.SurfaceAlt
        pnlSearch.Controls.AddRange({txtSearch, btnSearch, btnClear})
        Me.Controls.Add(pnlSearch)

        dgvSuppliers = New DataGridView With {
            .Location = New Point(24, 150),
            .Size = New Size(1060, 390),
            .BackgroundColor = Color.White,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }
        ModernTheme.StyleGrid(dgvSuppliers)
        dgvSuppliers.Columns.Add("SupplierName", "Supplier Name")
        dgvSuppliers.Columns.Add("Phone", "Contact No")
        dgvSuppliers.Columns.Add("Address", "Office Address")
        dgvSuppliers.Columns.Add("Records", "Records")
        dgvSuppliers.Columns("SupplierName").FillWeight = 30
        dgvSuppliers.Columns("Phone").FillWeight = 15
        dgvSuppliers.Columns("Address").FillWeight = 40
        dgvSuppliers.Columns("Records").FillWeight = 15
        Me.Controls.Add(dgvSuppliers)

        dgvPurchases = New DataGridView With {
            .Location = New Point(24, 150),
            .Size = New Size(1060, 390),
            .BackgroundColor = Color.White,
            .Visible = False,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }
        ModernTheme.StyleGrid(dgvPurchases)
        Me.Controls.Add(dgvPurchases)

        Dim pnlActions As New Panel With {.Location = New Point(24, 562), .Size = New Size(1060, 60), .BackColor = Color.Transparent}
        btnExport = ModernTheme.MakePrimaryButton("Export to CSV")
        btnExport.Location = New Point(0, 10)
        btnExport.Width = 150
        btnExport.BackColor = ModernTheme.Success
        btnViewRecords = ModernTheme.MakePrimaryButton("View records")
        btnViewRecords.Location = New Point(170, 10)
        btnViewRecords.Width = 150
        btnBackToSuppliers = ModernTheme.MakeSecondaryButton("Back to list")
        btnBackToSuppliers.Location = New Point(340, 10)
        btnBackToSuppliers.Width = 150
        btnBackToSuppliers.Visible = False
        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Close")
        btnClose.Location = New Point(500, 10)
        btnClose.Width = 110
        btnClose.ForeColor = ModernTheme.Danger
        AddHandler btnClose.Click, Sub() Me.Close()

        pnlActions.Controls.AddRange({btnExport, btnViewRecords, btnBackToSuppliers, btnClose})
        Me.Controls.Add(pnlActions)
    End Sub

    Private Sub Supplier_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        AddHandler btnSearch.Click, Sub() LoadSuppliers(txtSearch.Text.Trim())
        AddHandler btnClear.Click, Sub()
                                        txtSearch.Clear()
                                        LoadSuppliers("")
                                    End Sub
        AddHandler btnViewRecords.Click, AddressOf HandleViewRecords
        AddHandler btnBackToSuppliers.Click, Sub() ShowSupplierList()
        AddHandler btnExport.Click, AddressOf ExportToCSV
        AddHandler dgvSuppliers.CellClick, AddressOf HandleCellClick
        LoadSuppliers("")
    End Sub

    Private Sub LoadSuppliers(searchTerm As String)
        Dim list As New List(Of SupplierInfo)
        Try
            DBModule.OpenConnection()
            Dim sql As String = "SELECT * FROM mn.Supplier"
            If Not String.IsNullOrEmpty(searchTerm) Then
                sql &= " WHERE SupplierName LIKE @s OR Phone LIKE @s"
            End If
            Using cmd As New SqlCommand(sql, DBModule.Conn)
                cmd.Parameters.AddWithValue("@s", "%" & searchTerm & "%")
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        list.Add(New SupplierInfo With {
                            .SupplierID = CInt(reader("SupplierID")),
                            .SupplierName = reader("SupplierName").ToString(),
                            .Phone = If(IsDBNull(reader("Phone")), "N/A", reader("Phone").ToString()),
                            .Address = If(IsDBNull(reader("Address")), "N/A", reader("Address").ToString())
                        })
                    End While
                End Using
            End Using

            suppliersArray = list.ToArray()
            filteredArray = suppliersArray

            dgvSuppliers.Rows.Clear()
            For Each s In filteredArray
                Dim rowIndex As Integer = dgvSuppliers.Rows.Add(s.SupplierName, s.Phone, s.Address, "View records")
                dgvSuppliers.Rows(rowIndex).Tag = s.SupplierID
            Next
            btnViewRecords.Enabled = (dgvSuppliers.Rows.Count > 0)
        Catch ex As Exception
            MessageBox.Show("Error loading suppliers: " & ex.Message)
        Finally
            DBModule.CloseConnection()
        End Try
    End Sub

    Private Sub ShowSupplierList()
        viewMode = "LIST"
        dgvSuppliers.Visible = True
        dgvPurchases.Visible = False
        lblSupplierTitle.Visible = False
        btnBackToSuppliers.Visible = False
        lblTitle.Text = "Supplier information"
    End Sub

    Private Sub HandleViewRecords()
        If dgvSuppliers.SelectedRows.Count > 0 Then
            Dim id As Integer = CInt(dgvSuppliers.SelectedRows(0).Tag)
            Dim name As String = dgvSuppliers.SelectedRows(0).Cells(0).Value.ToString()
            LoadSupplierPurchases(id, name)
        End If
    End Sub

    Private Sub HandleCellClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 AndAlso e.ColumnIndex = 3 Then
            HandleViewRecords()
        End If
    End Sub

    Private Sub LoadSupplierPurchases(id As Integer, name As String)
        selectedSupplierID = id
        selectedSupplierName = name
        viewMode = "SUMMARY"

        Dim list As New List(Of PurchaseSummary)
        Try
            DBModule.OpenConnection()
            Dim sql As String = "SELECT CAST(PurchaseDate AS DATE) as PDate, SUM(QuantityPurchased) as TotalQty, SUM(QuantityPurchased * UnitCost) as TotalCost FROM mn.PurchaseBatch WHERE SupplierID = @id GROUP BY CAST(PurchaseDate AS DATE) ORDER BY PDate DESC"
            Using cmd As New SqlCommand(sql, DBModule.Conn)
                cmd.Parameters.AddWithValue("@id", id)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        list.Add(New PurchaseSummary With {
                            .PurchaseDate = CDate(reader("PDate")),
                            .TotalQuantityPurchased = CInt(reader("TotalQty")),
                            .TotalCost = CDec(reader("TotalCost"))
                        })
                    End While
                End Using
            End Using

            purchaseSummaryArray = list.ToArray()

            dgvPurchases.Columns.Clear()
            dgvPurchases.Columns.Add("Date", "Purchase Date")
            dgvPurchases.Columns.Add("Qty", "Items Received")
            dgvPurchases.Columns.Add("Cost", "Total Value")
            dgvPurchases.Rows.Clear()
            For Each p In purchaseSummaryArray
                dgvPurchases.Rows.Add(p.PurchaseDate.ToString("yyyy-MM-dd"), p.TotalQuantityPurchased, p.TotalCost.ToString("N2"))
            Next

            dgvSuppliers.Visible = False
            dgvPurchases.Visible = True
            lblSupplierTitle.Text = "Purchase history: " & name.ToUpper()
            lblSupplierTitle.Visible = True
            btnBackToSuppliers.Visible = True
        Catch ex As Exception
            MessageBox.Show("Error loading purchases: " & ex.Message)
        Finally
            DBModule.CloseConnection()
        End Try
    End Sub

    Private Sub ExportToCSV()
        Try
            Dim sb As New StringBuilder()
            If viewMode = "LIST" Then
                sb.AppendLine("Supplier Name,Phone,Address")
                For Each s In filteredArray
                    sb.AppendLine($"""{s.SupplierName}"",""{s.Phone}"",""{s.Address}""")
                Next
            Else
                sb.AppendLine("Purchase Date,Total Quantity,Total Cost")
                For Each p In purchaseSummaryArray
                    sb.AppendLine($"{p.PurchaseDate:yyyy-MM-dd},{p.TotalQuantityPurchased},{p.TotalCost}")
                Next
            End If

            Using sfd As New SaveFileDialog() With {.Filter = "CSV files (*.csv)|*.csv", .FileName = "Export_Data.csv"}
                If sfd.ShowDialog() = DialogResult.OK Then
                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString())
                    MessageBox.Show("Data exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("Export failed: " & ex.Message)
        End Try
    End Sub
End Class
