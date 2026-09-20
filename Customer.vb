Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Collections.Generic
Imports System.Text
Imports System.Drawing
Imports System.Windows.Forms

Public Class Customer
    Inherits Form ' Added inheritance

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

    ' UI Controls
    Private WithEvents dgvCustomers As DataGridView
    Private txtSearch As TextBox
    Private btnSearch As Button
    Private btnClear As Button
    Private btnExport As Button
    Private lblTitle As Label

    Public Sub New()
        SetupForm()
    End Sub

    Private Sub Customer_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadCustomers("")
    End Sub


    Private Sub SetupForm()
        Me.Text = "Customer Management System"
        Me.Size = New Size(1100, 600) ' Matches Supplier width
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(248, 249, 250)


        lblTitle = New Label With {
            .Text = "CUSTOMER DATABASE",
            .Font = New Font("Arial", 14, FontStyle.Bold),
            .ForeColor = Color.Navy,
            .Location = New Point(20, 20),
            .Size = New Size(400, 30)
        }
        Me.Controls.Add(lblTitle)


        Dim pnlSearch As New Panel With {
            .Location = New Point(20, 60),
            .Size = New Size(1045, 50),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle
        }

        txtSearch = New TextBox With {.Location = New Point(10, 12), .Size = New Size(300, 25), .Font = New Font("Arial", 10)}
        AddHandler txtSearch.KeyPress, AddressOf txtSearch_KeyPress

        btnSearch = New Button With {
            .Text = "Search", .Location = New Point(320, 10), .Size = New Size(80, 30),
            .BackColor = Color.FromArgb(59, 130, 246), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnSearch.Click, AddressOf btnSearch_Click

        btnClear = New Button With {
            .Text = "Clear", .Location = New Point(410, 10), .Size = New Size(80, 30),
            .BackColor = Color.Gray, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnClear.Click, AddressOf btnClear_Click

        pnlSearch.Controls.AddRange({txtSearch, btnSearch, btnClear})
        Me.Controls.Add(pnlSearch)


        dgvCustomers = New DataGridView With {
            .Location = New Point(20, 120),
            .Size = New Size(1045, 350),
            .BackgroundColor = Color.White,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill ' CRITICAL for full fit
        }


        dgvCustomers.Columns.Add("FullName", "Customer Name")
        dgvCustomers.Columns.Add("Phone", "Phone Number")
        dgvCustomers.Columns.Add("Email", "Email Address")
        dgvCustomers.Columns.Add("Address", "Home/Office Address")


        dgvCustomers.Columns("FullName").FillWeight = 25
        dgvCustomers.Columns("Phone").FillWeight = 15
        dgvCustomers.Columns("Email").FillWeight = 20
        dgvCustomers.Columns("Address").FillWeight = 40

        AddHandler dgvCustomers.CellFormatting, AddressOf dgvCustomers_CellFormatting
        Me.Controls.Add(dgvCustomers)


        btnExport = New Button With {
            .Text = "Export to CSV", .Location = New Point(20, 485), .Size = New Size(130, 40),
            .BackColor = Color.FromArgb(40, 167, 69), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnExport.Click, AddressOf btnExport_Click
        Me.Controls.Add(btnExport)

        Dim btnClose As New Button With {
            .Text = "Close", .Location = New Point(160, 485), .Size = New Size(100, 40),
            .BackColor = Color.IndianRed, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat
        }
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
            ' Reset column visibility in case it was hidden by "No Records" state
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
            ' Italicize and Color Red for "N/A" values
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