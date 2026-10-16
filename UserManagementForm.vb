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
        Me.Size = New Size(760, 620)
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
        btnCreate.Location = New Point(570, 500)
        btnCreate.Width = 150
        AddHandler btnCreate.Click, AddressOf CreateUser_Click
        Me.Controls.Add(btnCreate)

        Me.Controls.Add(New Label With {.Text = "Select a user in the list, then choose Reset password.", .Location = New Point(24, 552), .Size = New Size(560, 24), .ForeColor = ModernTheme.MutedText})

        Dim btnReset As Button = ModernTheme.MakeSecondaryButton("Reset password")
        btnReset.Location = New Point(400, 500)
        btnReset.Width = 150
        AddHandler btnReset.Click, AddressOf ResetPassword_Click
        Me.Controls.Add(btnReset)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Close")
        btnClose.Location = New Point(24, 500)
        btnClose.Width = 130
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)

        Me.AcceptButton = btnCreate
        Me.CancelButton = btnClose

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
        Try
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
        Catch ex As Exception
            AppLogger.Error("Failed to load roles", ex)
            MessageBox.Show("Unable to load roles: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        If cmbRole.Items.Count > 0 Then
            ' Default to the least-privileged common role rather than whatever
            ' happens to sort first, so a new account is not silently created
            ' with more access than intended.
            For Each preferred As String In New String() {"Storekeeper", "Clerk"}
                If cmbRole.Items.Contains(preferred) Then
                    cmbRole.SelectedItem = preferred
                    Return
                End If
            Next
            cmbRole.SelectedIndex = 0
        End If
    End Sub

    Private Sub LoadUsers()
        users.Clear()
        grid.Rows.Clear()
        Try
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
        Catch ex As Exception
            AppLogger.Error("Failed to load users", ex)
            MessageBox.Show("Unable to load users: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try
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
            AppLogger.Error("Failed to create user", ex)
            MessageBox.Show("Unable to create user: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ResetPassword_Click(sender As Object, e As EventArgs)
        If grid.SelectedRows.Count = 0 Then
            MessageBox.Show("Select a user in the list first.", "Reset password", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim row As DataGridViewRow = grid.SelectedRows(0)
        Dim userID As Integer = Convert.ToInt32(row.Cells("UserID").Value)
        Dim username As String = Convert.ToString(row.Cells("Username").Value)

        Dim newPassword As String = PromptForNewPassword(username)
        If newPassword Is Nothing Then Return

        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Using cmd As New SqlCommand("UPDATE mn.UUser SET PasswordHash = @hash WHERE UserID = @userID", conn, transaction)
                        cmd.Parameters.AddWithValue("@hash", SecurityService.HashPassword(newPassword))
                        cmd.Parameters.AddWithValue("@userID", userID)
                        cmd.ExecuteNonQuery()
                    End Using
                    AccountingService.WriteAudit(conn, transaction, "USER_PASSWORD_RESET", "UUser", userID, "Administrator reset the password for " & username)
                    transaction.Commit()
                End Using
            End Using
            MessageBox.Show($"The password for '{username}' was reset.{Environment.NewLine}{Environment.NewLine}Share it securely and ask them to change it after signing in.", "Reset password", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            AppLogger.Error("Failed to reset user password", ex)
            MessageBox.Show("Unable to reset the password: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Asks the administrator for a new password (masked) and returns it, or
    ''' Nothing if the dialog was cancelled.
    ''' </summary>
    Private Function PromptForNewPassword(username As String) As String
        Using dialog As New Form()
            dialog.Text = "Reset password"
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog
            dialog.StartPosition = FormStartPosition.CenterParent
            dialog.MaximizeBox = False
            dialog.MinimizeBox = False
            dialog.ClientSize = New Size(380, 215)
            ModernTheme.ApplyWindowTheme(dialog)

            dialog.Controls.Add(New Label With {.Text = "New password for " & username, .Font = New Font("Segoe UI", 12, FontStyle.Bold), .Location = New Point(20, 18), .AutoSize = True})
            dialog.Controls.Add(New Label With {.Text = "New password", .Location = New Point(20, 62), .Size = New Size(120, 24), .ForeColor = ModernTheme.MutedText})
            dialog.Controls.Add(New Label With {.Text = "Confirm password", .Location = New Point(20, 100), .Size = New Size(120, 24), .ForeColor = ModernTheme.MutedText})

            Dim txtNew As New TextBox With {.Location = New Point(150, 60), .Width = 210, .PasswordChar = "*"c}
            Dim txtConfirm As New TextBox With {.Location = New Point(150, 98), .Width = 210, .PasswordChar = "*"c}
            dialog.Controls.Add(txtNew)
            dialog.Controls.Add(txtConfirm)

            Dim lblError As New Label With {.Location = New Point(20, 130), .Size = New Size(340, 22), .ForeColor = ModernTheme.Danger}
            dialog.Controls.Add(lblError)

            Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("Cancel")
            btnCancel.Location = New Point(100, 158)
            btnCancel.Width = 100
            btnCancel.DialogResult = DialogResult.Cancel

            Dim btnOk As Button = ModernTheme.MakePrimaryButton("Reset")
            btnOk.Location = New Point(210, 158)
            btnOk.Width = 150
            btnOk.DialogResult = DialogResult.None

            dialog.Controls.Add(btnCancel)
            dialog.Controls.Add(btnOk)
            dialog.AcceptButton = btnOk
            dialog.CancelButton = btnCancel

            Dim result As String = Nothing
            AddHandler btnOk.Click, Sub(sender, e)
                                        If txtNew.Text.Length < 6 Then
                                            lblError.Text = "Password must be at least 6 characters."
                                            Return
                                        End If
                                        If txtNew.Text <> txtConfirm.Text Then
                                            lblError.Text = "Passwords do not match."
                                            Return
                                        End If
                                        result = txtNew.Text
                                        dialog.DialogResult = DialogResult.OK
                                    End Sub

            If dialog.ShowDialog(Me) = DialogResult.OK Then Return result
            Return Nothing
        End Using
    End Function

    ' Legacy hash function removed, PBKDF2 will be used instead
    ' Private Function HashPassword(password As String) As String
    '     Using sha256 As SHA256 = SHA256.Create()
    '         Return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)))
    '     End Using
    ' End Function
End Class
