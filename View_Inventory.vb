Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Drawing
Imports System.Windows.Forms

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

    Private WithEvents dgvInventory As DataGridView
    Private txtSearch As TextBox
    Private btnSearch As Button
    Private btnClear As Button
    Private lstCategories As ListBox
    Private btnFilterCategory As Button
    Private WithEvents btnSave As Button
    Private WithEvents btnCancel As Button

    Private Sub View_Inventory_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ModernTheme.ApplyWindowTheme(Me)
        SetupForm()
        LoadCategories()
        LoadInventory()
    End Sub

    Private Sub SetupForm()
        Me.Text = "Inventory Management"
        Me.Size = New Size(1080, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        Dim lblTitle As New Label With {
            .Text = "Spare parts inventory",
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
        txtSearch = ModernTheme.MakeTextBox()
        txtSearch.Location = New Point(12, 8)
        txtSearch.Width = 380
        pnlSearch.Controls.Add(txtSearch)

        btnSearch = ModernTheme.MakePrimaryButton("Search")
        btnSearch.Location = New Point(406, 8)
        btnSearch.Width = 120
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlSearch.Controls.Add(btnSearch)

        btnClear = ModernTheme.MakeSecondaryButton("Clear all")
        btnClear.Location = New Point(536, 8)
        btnClear.Width = 120
        btnClear.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnClear.Click, AddressOf btnClear_Click
        pnlSearch.Controls.Add(btnClear)
        Me.Controls.Add(pnlSearch)

        Dim pnlCategory As New Panel With {
            .Location = New Point(22, 130),
            .Size = New Size(210, 420),
            .BackColor = Color.White
        }
        Dim lblCategory As New Label With {
            .Text = "Categories",
            .Location = New Point(12, 16),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        pnlCategory.Controls.Add(lblCategory)

        lstCategories = New ListBox With {
            .Location = New Point(12, 42),
            .Size = New Size(180, 300),
            .BorderStyle = BorderStyle.None,
            .Font = New Font("Segoe UI", 10)
        }
        pnlCategory.Controls.Add(lstCategories)

        btnFilterCategory = ModernTheme.MakePrimaryButton("Apply category")
        btnFilterCategory.Location = New Point(12, 360)
        btnFilterCategory.Width = 180
        AddHandler btnFilterCategory.Click, AddressOf btnFilterCategory_Click
        pnlCategory.Controls.Add(btnFilterCategory)
        Me.Controls.Add(pnlCategory)

        dgvInventory = New DataGridView With {
            .Location = New Point(252, 130),
            .Size = New Size(780, 420),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle With {.BackColor = ModernTheme.SurfaceAlt},
            .Font = New Font("Segoe UI", 10)
        }
        ModernTheme.StyleGrid(dgvInventory)
        dgvInventory.Columns.Add("Description", "Description")
        dgvInventory.Columns.Add("PartNumber", "Part Number")
        dgvInventory.Columns.Add("Category", "Category")
        dgvInventory.Columns.Add("Quantity", "Stock")
        dgvInventory.Columns.Add("UnitPrice", "Price")
        dgvInventory.Columns("Description").FillWeight = 150
        dgvInventory.Columns("UnitPrice").DefaultCellStyle.Format = "C2"
        AddHandler dgvInventory.CellDoubleClick, AddressOf dgvInventory_CellDoubleClick
        Me.Controls.Add(dgvInventory)

        btnSave = ModernTheme.MakePrimaryButton("Save changes")
        btnSave.Location = New Point(762, 570)
        btnSave.Width = 150
        btnSave.Visible = False
        Me.Controls.Add(btnSave)

        btnCancel = ModernTheme.MakeSecondaryButton("Cancel")
        btnCancel.Location = New Point(920, 570)
        btnCancel.Width = 110
        btnCancel.Visible = False
        btnCancel.BackColor = ModernTheme.SurfaceAlt
        btnCancel.ForeColor = ModernTheme.Danger
        Me.Controls.Add(btnCancel)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Back to dashboard")
        btnClose.Location = New Point(22, 570)
        btnClose.Width = 180
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
            While reader.Read()
                lstCategories.Items.Add(reader(0).ToString())
            End While
            DBModule.CloseConnection()
        Catch : End Try
    End Sub

    Private Sub dgvInventory_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then
            editRowIndex = e.RowIndex
            dgvInventory.ReadOnly = False
            dgvInventory.Columns("PartNumber").ReadOnly = True
            dgvInventory.Columns("Category").ReadOnly = True
            btnSave.Visible = True
            btnCancel.Visible = True
        End If
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If editRowIndex < 0 OrElse editRowIndex >= dgvInventory.Rows.Count Then Return

        Dim row As DataGridViewRow = dgvInventory.Rows(editRowIndex)
        Dim partNumber As String = Convert.ToString(row.Cells("PartNumber").Value).Trim()
        Dim description As String = Convert.ToString(row.Cells("Description").Value).Trim()
        Dim quantity As Integer
        Dim unitPrice As Decimal

        If String.IsNullOrEmpty(description) OrElse
           Not Integer.TryParse(Convert.ToString(row.Cells("Quantity").Value), quantity) OrElse quantity < 0 OrElse
           Not Decimal.TryParse(Convert.ToString(row.Cells("UnitPrice").Value), unitPrice) OrElse unitPrice < 0 Then
            MessageBox.Show("Enter a description, a non-negative quantity, and a non-negative price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Const query As String = "UPDATE mn.SpareParts SET Description = @description, Quantity = @quantity, UnitPrice = @unitPrice WHERE PartNumber = @partNumber"
                Using cmd As New SqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@description", description)
                    cmd.Parameters.AddWithValue("@quantity", quantity)
                    cmd.Parameters.AddWithValue("@unitPrice", unitPrice)
                    cmd.Parameters.AddWithValue("@partNumber", partNumber)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
            MessageBox.Show("Inventory item saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            LoadInventory()
        Catch ex As Exception
            MessageBox.Show("Unable to save inventory item: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

        dgvInventory.ReadOnly = True
        btnSave.Visible = False
        btnCancel.Visible = False
        editRowIndex = -1
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        LoadInventory()
        dgvInventory.ReadOnly = True
        btnSave.Visible = False
        btnCancel.Visible = False
        editRowIndex = -1
    End Sub
End Class
