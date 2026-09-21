Imports System.Drawing
Imports System.Windows.Forms
Imports System.Data.SqlClient
Imports System.Text

Public Class Customer
    Inherits Form

    Public Structure CustomerInfo
        Public CustomerID As Integer
        Public FullName As String
        Public Phone As String
        Public Email As String
        Public Address As String
    End Structure

    Private customersArray() As CustomerInfo
    Private filteredArray() As CustomerInfo
    Private totalCustomers As Integer = 0

    Private WithEvents dgvCustomers As DataGridView
    Private txtSearch As TextBox
    Private btnSearch As Button
    Private btnClear As Button
    Private btnExport As Button
    Private lblTitle As Label

    Public Sub New()
        ModernTheme.ApplyWindowTheme(Me)
        SetupForm()
    End Sub

    Private Sub Customer_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadCustomers("")
    End Sub

    Private Sub SetupForm()
        Me.Text = "Customer Management System"
        Me.Size = New Size(1120, 620)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        ModernTheme.EnableVerticalScroll(Me, 700)

        lblTitle = New Label With {
            .Text = "Customer database",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(24, 18),
            .Size = New Size(300, 34)
        }
        Me.Controls.Add(lblTitle)

        Dim pnlSearch As New Panel With {
            .Location = New Point(24, 70),
            .Size = New Size(1060, 58),
            .BackColor = Color.White
        }

        txtSearch = ModernTheme.MakeTextBox()
        txtSearch.Location = New Point(12, 12)
        txtSearch.Width = 360
        AddHandler txtSearch.KeyPress, AddressOf txtSearch_KeyPress
        pnlSearch.Controls.Add(txtSearch)

        btnSearch = ModernTheme.MakePrimaryButton("Search")
        btnSearch.Location = New Point(386, 10)
        btnSearch.Width = 120
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlSearch.Controls.Add(btnSearch)

        btnClear = ModernTheme.MakeSecondaryButton("Clear")
        btnClear.Location = New Point(518, 10)
        btnClear.Width = 110
        btnClear.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnClear.Click, AddressOf btnClear_Click
        pnlSearch.Controls.Add(btnClear)

        Me.Controls.Add(pnlSearch)

        dgvCustomers = New DataGridView With {
            .Location = New Point(24, 150),
            .Size = New Size(1060, 360),
            .BackgroundColor = Color.White,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }
        ModernTheme.StyleGrid(dgvCustomers)
        dgvCustomers.Columns.Add("FullName", "Customer Name")
        dgvCustomers.Columns.Add("Phone", "Phone Number")
        dgvCustomers.Columns.Add("Email", "Email Address")
        dgvCustomers.Columns.Add("Address", "Home/Office Address")
        AddHandler dgvCustomers.CellFormatting, AddressOf dgvCustomers_CellFormatting
        Me.Controls.Add(dgvCustomers)

        btnExport = ModernTheme.MakePrimaryButton("Export to CSV")
        btnExport.Location = New Point(24, 530)
        btnExport.Width = 160
        btnExport.BackColor = ModernTheme.Success
        AddHandler btnExport.Click, AddressOf btnExport_Click
        Me.Controls.Add(btnExport)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Close")
        btnClose.Location = New Point(200, 530)
        btnClose.Width = 120
        btnClose.BackColor = ModernTheme.SurfaceAlt
        btnClose.ForeColor = ModernTheme.Danger
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)
    End Sub

    Private Function GetCustomersFromDB(Optional ByVal searchTerm As String = "") As CustomerInfo()
        Dim customersList As New List(Of CustomerInfo)
        Try
            DBModule.OpenConnection()
            Dim query As String = "SELECT CustomerID, CustomerName, Phone, Email, Address FROM mn.Customer"
            If Not String.IsNullOrEmpty(searchTerm) Then
                query &= " WHERE CustomerName LIKE @s OR Phone LIKE @s OR Email LIKE @s"
            End If
            query &= " ORDER BY CustomerName"

            Using cmd As New SqlCommand(query, DBModule.Conn)
                cmd.Parameters.AddWithValue("@s", "%" & searchTerm & "%")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    Do While reader.Read()
                        customersList.Add(New CustomerInfo With {
                            .CustomerID = CInt(reader("CustomerID")),
                            .FullName = reader("CustomerName").ToString(),
                            .Phone = If(IsDBNull(reader("Phone")), "N/A", reader("Phone").ToString()),
                            .Email = If(IsDBNull(reader("Email")), "N/A", reader("Email").ToString()),
                            .Address = If(IsDBNull(reader("Address")), "N/A", reader("Address").ToString())
                        })
                    Loop
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("DB Error: " & ex.Message)
        Finally
            DBModule.CloseConnection()
        End Try
        Return customersList.ToArray()
    End Function

    Private Sub LoadCustomers(Optional ByVal searchTerm As String = "")
        customersArray = GetCustomersFromDB(searchTerm)
        filteredArray = customersArray

        dgvCustomers.Rows.Clear()
        If filteredArray.Length > 0 Then
            For Each col As DataGridViewColumn In dgvCustomers.Columns : col.Visible = True : Next
            For i As Integer = 0 To filteredArray.Length - 1
                dgvCustomers.Rows.Add(filteredArray(i).FullName, filteredArray(i).Phone, filteredArray(i).Email, filteredArray(i).Address)
            Next
        Else
            dgvCustomers.Rows.Add("No customers found matching your search.")
            dgvCustomers.Columns(1).Visible = False
            dgvCustomers.Columns(2).Visible = False
            dgvCustomers.Columns(3).Visible = False
        End If
        totalCustomers = filteredArray.Length
    End Sub

    Private Sub dgvCustomers_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex >= 0 Then
            If e.Value IsNot Nothing AndAlso e.Value.ToString() = "N/A" Then
                e.CellStyle.ForeColor = Color.Red
                e.CellStyle.Font = New Font(dgvCustomers.Font, FontStyle.Italic)
            End If
        End If
    End Sub

    Private Sub btnSearch_Click(sender As Object, e As EventArgs)
        LoadCustomers(txtSearch.Text.Trim())
    End Sub

    Private Sub txtSearch_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            LoadCustomers(txtSearch.Text.Trim())
            e.Handled = True
        End If
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs)
        txtSearch.Clear()
        LoadCustomers("")
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs)
        Try
            Dim sb As New StringBuilder()
            sb.AppendLine("Name,Phone,Email,Address")
            For Each c In filteredArray
                sb.AppendLine($"""{c.FullName}"",""{c.Phone}"",""{c.Email}"",""{c.Address}""")
            Next

            Using sfd As New SaveFileDialog() With {.Filter = "CSV|*.csv", .FileName = "Customer_List.csv"}
                If sfd.ShowDialog() = DialogResult.OK Then
                    IO.File.WriteAllText(sfd.FileName, sb.ToString())
                    MessageBox.Show("Export Successful!")
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("Export Error: " & ex.Message)
        End Try
    End Sub
End Class
