Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq

Public Class View_Inventory

    Public Structure InventoryItem
        Public PartNumber As String
        Public Description As String
        Public CategoryName As String
        Public Quantity As Integer
        Public UnitPrice As Decimal
    End Structure

    Private inventoryArray() As InventoryItem
    Private filteredArray() As InventoryItem

    Private isEditing As Boolean = False
    Private editRowIndex As Integer = -1

    ' UI Controls
    Private WithEvents dgvInventory As DataGridView
    Private txtSearch As TextBox
    Private btnSearch As Button
    Private btnClear As Button
    Private lstCategories As ListBox
    Private btnFilterCategory As Button
    Private WithEvents btnSave As Button
    Private WithEvents btnCancel As Button

    Private Sub View_Inventory_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupForm()
        LoadCategories()
        LoadInventory()
    End Sub


    Private Sub SetupForm()
        Me.Text = "Inventory Management"
        Me.Size = New Size(1000, 650) ' Widened to accommodate a full table
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(244, 246, 248)

        ' 1. Title
        Dim lblTitle As New Label With {
            .Text = "SPARE PARTS INVENTORY",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.FromArgb(21, 101, 192),
            .Location = New Point(20, 15),
            .Size = New Size(400, 35)
        }
        Me.Controls.Add(lblTitle)

        ' 2. Search Panel (Top)
        Dim pnlSearch As New Panel With {
            .Location = New Point(20, 60),
            .Size = New Size(940, 50),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.None
        }
        txtSearch = New TextBox With {.Location = New Point(15, 12), .Size = New Size(350, 25), .Font = New Font("Segoe UI", 10)}
        pnlSearch.Controls.Add(txtSearch)

        btnSearch = New Button With {
            .Text = "Search", .Location = New Point(380, 10), .Size = New Size(100, 30),
            .BackColor = Color.FromArgb(59, 130, 246), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlSearch.Controls.Add(btnSearch)

        btnClear = New Button With {
            .Text = "Clear All", .Location = New Point(490, 10), .Size = New Size(100, 30),
            .BackColor = Color.FromArgb(108, 117, 125), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnClear.Click, AddressOf btnClear_Click
        pnlSearch.Controls.Add(btnClear)
        Me.Controls.Add(pnlSearch)

        ' 3. Category Filter (Side)
        Dim pnlCategory As New Panel With {
            .Location = New Point(20, 125), .Size = New Size(200, 400), .BackColor = Color.White
        }
        Dim lblCategory As New Label With {.Text = "CATEGORIES", .Location = New Point(10, 15), .Font = New Font("Segoe UI", 9, FontStyle.Bold), .ForeColor = Color.Gray}
        pnlCategory.Controls.Add(lblCategory)

        lstCategories = New ListBox With {
            .Location = New Point(10, 45), .Size = New Size(180, 300), .BorderStyle = BorderStyle.None, .Font = New Font("Segoe UI", 10)
        }
        pnlCategory.Controls.Add(lstCategories)

        btnFilterCategory = New Button With {
            .Text = "Apply Category", .Location = New Point(10, 355), .Size = New Size(180, 35),
            .BackColor = Color.FromArgb(34, 197, 94), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnFilterCategory.Click, AddressOf btnFilterCategory_Click
        pnlCategory.Controls.Add(btnFilterCategory)
        Me.Controls.Add(pnlCategory)


        dgvInventory = New DataGridView With {
            .Location = New Point(235, 125),
            .Size = New Size(725, 400),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ' <--- THIS MAKES IT FIT FULLY
            .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle With {.BackColor = Color.FromArgb(245, 247, 250)},
            .Font = New Font("Segoe UI", 10)
        }


        dgvInventory.Columns.Add("Description", "Description")
        dgvInventory.Columns.Add("PartNumber", "Part Number")
        dgvInventory.Columns.Add("Category", "Category")
        dgvInventory.Columns.Add("Quantity", "Stock")
        dgvInventory.Columns.Add("UnitPrice", "Price")


        dgvInventory.Columns("Description").FillWeight = 150 ' Takes more space
        dgvInventory.Columns("UnitPrice").DefaultCellStyle.Format = "C2"

        AddHandler dgvInventory.CellDoubleClick, AddressOf dgvInventory_CellDoubleClick
        Me.Controls.Add(dgvInventory)


        btnSave = New Button With {
            .Text = "Save Changes", .Location = New Point(735, 540), .Size = New Size(110, 35),
            .BackColor = Color.FromArgb(34, 197, 94), .ForeColor = Color.White, .Visible = False, .FlatStyle = FlatStyle.Flat
        }
        Me.Controls.Add(btnSave)

        btnCancel = New Button With {
            .Text = "Cancel", .Location = New Point(850, 540), .Size = New Size(110, 35),
            .BackColor = Color.FromArgb(239, 68, 68), .ForeColor = Color.White, .Visible = False, .FlatStyle = FlatStyle.Flat
        }
        Me.Controls.Add(btnCancel)

        Dim btnClose As New Button With {
            .Text = "Back to Dashboard", .Location = New Point(20, 540), .Size = New Size(200, 35),
            .BackColor = Color.White, .ForeColor = Color.FromArgb(33, 33, 33), .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)
    End Sub


    Private Function GetInventoryFromDB() As InventoryItem()
        Dim itemsList As New List(Of InventoryItem)
        Try
            DBModule.OpenConnection()
            Dim query As String = "SELECT s.Description, s.PartNumber, c.CategoryName, s.Quantity, s.UnitPrice FROM mn.SpareParts s JOIN mn.Category c ON s.CategoryID = c.CategoryID ORDER BY s.PartNumber"
            Using cmd As New SqlCommand(query, DBModule.Conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    Do While reader.Read()
                        itemsList.Add(New InventoryItem With {
                            .PartNumber = reader("PartNumber").ToString(),
                            .Description = reader("Description").ToString(),
                            .CategoryName = reader("CategoryName").ToString(),
                            .Quantity = CInt(reader("Quantity")),
                            .UnitPrice = CDec(reader("UnitPrice"))
                        })
                    Loop
                End Using
            End Using
            DBModule.CloseConnection()
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
        Return itemsList.ToArray()
    End Function

    Private Sub LoadInventory()
        inventoryArray = GetInventoryFromDB()
        filteredArray = inventoryArray
        DisplayInventory(filteredArray)
    End Sub

    Private Sub DisplayInventory(items As InventoryItem())
        dgvInventory.Rows.Clear()
        For Each item In items
            dgvInventory.Rows.Add(item.Description, item.PartNumber, item.CategoryName, item.Quantity, item.UnitPrice)
        Next
    End Sub


    Private Sub btnSearch_Click(sender As Object, e As EventArgs)
        Dim s = txtSearch.Text.Trim().ToLower()
        If String.IsNullOrEmpty(s) Then
            filteredArray = inventoryArray
        Else
            filteredArray = (From i In inventoryArray Where i.PartNumber.ToLower().Contains(s) Or i.Description.ToLower().Contains(s) Select i).ToArray()
        End If
        DisplayInventory(filteredArray)
    End Sub

    Private Sub btnFilterCategory_Click(sender As Object, e As EventArgs)
        If lstCategories.SelectedIndex <> -1 Then
            Dim cat = lstCategories.SelectedItem.ToString()
            filteredArray = (From i In inventoryArray Where i.CategoryName = cat Select i).ToArray()
            DisplayInventory(filteredArray)
        End If
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs)
        txtSearch.Clear()
        lstCategories.ClearSelected()
        LoadInventory()
    End Sub

    Private Sub LoadCategories()
        Try
            DBModule.OpenConnection()
            Dim cmd As New SqlCommand("SELECT CategoryName FROM mn.Category", DBModule.Conn)
            Dim reader = cmd.ExecuteReader()
            lstCategories.Items.Clear()
            While reader.Read() : lstCategories.Items.Add(reader(0).ToString()) : End While
            DBModule.CloseConnection()
        Catch : End Try
    End Sub

    Private Sub dgvInventory_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then
            dgvInventory.ReadOnly = False
            btnSave.Visible = True
            btnCancel.Visible = True
        End If
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        ' Logic to save row changes back to DB would go here
        dgvInventory.ReadOnly = True
        btnSave.Visible = False
        btnCancel.Visible = False
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        LoadInventory()
        dgvInventory.ReadOnly = True
        btnSave.Visible = False
        btnCancel.Visible = False
    End Sub
End Class