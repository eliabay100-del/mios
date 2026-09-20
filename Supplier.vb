Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Collections.Generic
Imports System.Text
Imports System.Drawing
Imports System.Windows.Forms

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
    Private viewMode As String = "LIST" ' LIST or SUMMARY
    Private selectedSupplierID As Integer = -1
    Private selectedSupplierName As String = ""

    ' UI Controls
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
        Me.Text = "Supplier Management"
        Me.Size = New Size(1100, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(248, 249, 250)
        InitializeSupplierForm()
    End Sub


    Private Sub InitializeSupplierForm()
        ' Main Title
        lblTitle = New Label With {
            .Text = "SUPPLIER INFORMATION",
            .Font = New Font("Arial", 14, FontStyle.Bold),
            .ForeColor = Color.Navy,
            .Location = New Point(20, 20),
            .Size = New Size(400, 30)
        }
        Me.Controls.Add(lblTitle)

        ' Secondary Title (for records view)
        lblSupplierTitle = New Label With {
            .Font = New Font("Arial", 12, FontStyle.Bold),
            .ForeColor = Color.DarkGreen,
            .Location = New Point(20, 60),
            .Size = New Size(800, 25),
            .Visible = False
        }
        Me.Controls.Add(lblSupplierTitle)

        lblSearchLabel = New Label With {
            .Text = "Search Supplier:",
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .Location = New Point(20, 100),
            .Size = New Size(150, 25)
        }
        Me.Controls.Add(lblSearchLabel)

        Dim pnlSearch As New Panel With {.Location = New Point(170, 95), .Size = New Size(900, 40)}
        txtSearch = New TextBox With {.Location = New Point(10, 7), .Size = New Size(300, 25), .Font = New Font("Arial", 10)}
        btnSearch = New Button With {.Text = "Search", .Location = New Point(320, 5), .Size = New Size(80, 30), .BackColor = Color.FromArgb(59, 130, 246), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
        btnClear = New Button With {.Text = "Clear", .Location = New Point(410, 5), .Size = New Size(80, 30), .BackColor = Color.Gray, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
        pnlSearch.Controls.AddRange({txtSearch, btnSearch, btnClear})
        Me.Controls.Add(pnlSearch)


        dgvSuppliers = New DataGridView With {
            .Location = New Point(20, 150),
            .Size = New Size(1050, 350),
            .BackgroundColor = Color.White,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }
        dgvSuppliers.Columns.Add("SupplierName", "Supplier Name")
        dgvSuppliers.Columns.Add("Phone", "Contact No")
        dgvSuppliers.Columns.Add("Address", "Office Address")
        dgvSuppliers.Columns.Add("Records", "Records")

        ' Adjust column ratios (Total FillWeight = 100)
        dgvSuppliers.Columns("SupplierName").FillWeight = 30
        dgvSuppliers.Columns("Phone").FillWeight = 15
        dgvSuppliers.Columns("Address").FillWeight = 40
        dgvSuppliers.Columns("Records").FillWeight = 15
        Me.Controls.Add(dgvSuppliers)

        dgvPurchases = New DataGridView With {
            .Location = New Point(20, 150),
            .Size = New Size(1050, 350),
            .BackgroundColor = Color.White,
            .Visible = False,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }
        Me.Controls.Add(dgvPurchases)


        Dim pnlActions As New Panel With {.Location = New Point(20, 520), .Size = New Size(1050, 60)}
        btnExport = New Button With {.Text = "Export to CSV", .Location = New Point(0, 10), .Size = New Size(130, 40), .BackColor = Color.FromArgb(40, 167, 69), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
        btnViewRecords = New Button With {.Text = "View Records", .Location = New Point(140, 10), .Size = New Size(130, 40), .BackColor = Color.FromArgb(23, 162, 184), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Enabled = False}
        btnBackToSuppliers = New Button With {.Text = "Back to List", .Location = New Point(280, 10), .Size = New Size(130, 40), .BackColor = Color.Gray, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Visible = False}

        Dim btnClose As New Button With {.Text = "Close", .Location = New Point(420, 10), .Size = New Size(100, 40), .BackColor = Color.IndianRed, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
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
                Dim rowIndex As Integer = dgvSuppliers.Rows.Add(s.SupplierName, s.Phone, s.Address, "View Records")
                dgvSuppliers.Rows(rowIndex).Tag = s.SupplierID
            Next
            btnViewRecords.Enabled = (dgvSuppliers.Rows.Count > 0)
        Catch ex As Exception
            MessageBox.Show("Error loading suppliers: " & ex.Message)
        Finally
            DBModule.CloseConnection()
        End Try
    End Sub

    ' Navigate back to the main list
    Private Sub ShowSupplierList()
        viewMode = "LIST"
        dgvSuppliers.Visible = True
        dgvPurchases.Visible = False
        lblSupplierTitle.Visible = False
        btnBackToSuppliers.Visible = False
        lblTitle.Text = "SUPPLIER INFORMATION"
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
            Dim sql As String = "SELECT CAST(PurchaseDate AS DATE) as PDate, " &
                               "SUM(QuantityPurchased) as TotalQty, " &
                               "SUM(QuantityPurchased * UnitCost) as TotalCost " &
                               "FROM mn.PurchaseBatch WHERE SupplierID = @id " &
                               "GROUP BY CAST(PurchaseDate AS DATE) ORDER BY PDate DESC"

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

            ' Prepare Purchase Grid
            dgvPurchases.Columns.Clear()
            dgvPurchases.Columns.Add("Date", "Purchase Date")
            dgvPurchases.Columns.Add("Qty", "Items Received")
            dgvPurchases.Columns.Add("Cost", "Total Value (ETB)")

            dgvPurchases.Rows.Clear()
            For Each p In purchaseSummaryArray
                dgvPurchases.Rows.Add(p.PurchaseDate.ToString("yyyy-MM-dd"), p.TotalQuantityPurchased, p.TotalCost.ToString("N2"))
            Next

            ' Update UI Visibility
            dgvSuppliers.Visible = False
            dgvPurchases.Visible = True
            lblSupplierTitle.Text = "PURCHASE HISTORY: " & name.ToUpper()
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