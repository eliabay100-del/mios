Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Security.Cryptography
Imports System.Text
Imports System.Windows.Forms

Public Class UserManagementForm
    Inherits Form

    Private ReadOnly grid As New DataGridView()
    Private ReadOnly txtUsername As New TextBox()
    Private ReadOnly txtPassword As New TextBox()
    Private ReadOnly cmbRole As New ComboBox()
    Private users As New List(Of UserRow)()

    Private Class UserRow
        Public Property UserID As Integer
        Public Property Username As String
        Public Property RoleName As String
    End Class

    Public Sub New()
        If Not SecurityContext.HasPermission("USER_MANAGE") Then Throw New UnauthorizedAccessException("Only administrators can manage users.")
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Users and roles"
        Me.Size = New Size(760, 560)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        Me.Controls.Add(New Label With {.Text = "Users and roles", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .Location = New Point(24, 22), .AutoSize = True})
        grid.Location = New Point(24, 76)
        grid.Size = New Size(700, 270)
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        grid.Columns.Add("UserID", "ID")
        grid.Columns.Add("Username", "Username")
        grid.Columns.Add("Role", "Role")
        ModernTheme.StyleGrid(grid)
        Me.Controls.Add(grid)

        AddField("New username", txtUsername, 370)
        AddField("Password", txtPassword, 415)
        txtPassword.PasswordChar = "*"c
        AddField("Role", cmbRole, 460)
        cmbRole.DropDownStyle = ComboBoxStyle.DropDownList

        Dim btnCreate As Button = ModernTheme.MakePrimaryButton("Create user")
        btnCreate.Location = New Point(570, 450)
        btnCreate.Width = 150
        AddHandler btnCreate.Click, AddressOf CreateUser_Click
        Me.Controls.Add(btnCreate)
        LoadUsers()
        LoadRoles()
    End Sub

    Private Sub AddField(labelText As String, control As Control, top As Integer)
        Me.Controls.Add(New Label With {.Text = labelText, .Location = New Point(24, top + 4), .Size = New Size(120, 24), .ForeColor = ModernTheme.MutedText})
        control.Location = New Point(155, top)
        control.Width = 330
        Me.Controls.Add(control)
    End Sub

    Private Sub LoadRoles()
        Using conn As New SqlConnection(DBModule.ConnString)
            conn.Open()
            Using cmd As New SqlCommand("SELECT RoleName FROM mn.Role ORDER BY RoleName", conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        cmbRole.Items.Add(reader.GetString(0))
                    End While
                End Using
            End Using
        End Using
        If cmbRole.Items.Count > 0 Then cmbRole.SelectedItem = "Storekeeper"
    End Sub

    Private Sub LoadUsers()
        users.Clear()
        grid.Rows.Clear()
        Using conn As New SqlConnection(DBModule.ConnString)
            conn.Open()
            Const query As String = "SELECT u.UserID, u.Username, ISNULL(r.RoleName, 'Unassigned') FROM mn.UUser u LEFT JOIN mn.UserRole ur ON ur.UserID = u.UserID LEFT JOIN mn.Role r ON r.RoleID = ur.RoleID ORDER BY u.Username"
            Using cmd As New SqlCommand(query, conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        users.Add(New UserRow With {.UserID = reader.GetInt32(0), .Username = reader.GetString(1), .RoleName = reader.GetString(2)})
                    End While
                End Using
            End Using
        End Using
        For Each user In users
            grid.Rows.Add(user.UserID, user.Username, user.RoleName)
        Next
    End Sub

    Private Sub CreateUser_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtUsername.Text) OrElse txtPassword.Text.Length < 6 OrElse cmbRole.SelectedIndex < 0 Then
            MessageBox.Show("Enter a username, a password of at least 6 characters, and a role.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Dim userID As Integer
                    Using cmd As New SqlCommand("INSERT INTO mn.UUser (Username, PasswordHash) OUTPUT INSERTED.UserID VALUES (@username, @hash)", conn, transaction)
                        cmd.Parameters.AddWithValue("@username", txtUsername.Text.Trim())
                        cmd.Parameters.AddWithValue("@hash", HashPassword(txtPassword.Text))
                            cmd.Parameters.AddWithValue("@hash", SecurityService.HashPassword(txtPassword.Text))
                        userID = Convert.ToInt32(cmd.ExecuteScalar())
                    End Using
                    Using cmd As New SqlCommand("INSERT INTO mn.UserRole (UserID, RoleID) SELECT @userID, RoleID FROM mn.Role WHERE RoleName = @role", conn, transaction)
                        cmd.Parameters.AddWithValue("@userID", userID)
                        cmd.Parameters.AddWithValue("@role", cmbRole.Text)
                        cmd.ExecuteNonQuery()
                    End Using
                    AccountingService.WriteAudit(conn, transaction, "USER_CREATED", "UUser", userID, "Created user with role " & cmbRole.Text)
                    transaction.Commit()
                End Using
            End Using
            txtUsername.Clear()
            txtPassword.Clear()
            LoadUsers()
        Catch ex As Exception
            MessageBox.Show("Unable to create user: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Legacy hash function removed, PBKDF2 will be used instead
    ' Private Function HashPassword(password As String) As String
    '     Using sha256 As SHA256 = SHA256.Create()
    '         Return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)))
    '     End Using
    ' End Function
End Class
