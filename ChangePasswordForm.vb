Imports System.Drawing
Imports System.Windows.Forms
Imports System.Data.SqlClient
Imports System.Text

Public Class ChangePasswordForm
    Private txtCurrentUsername As TextBox
    Private txtCurrentPassword As TextBox
    Private txtNewPassword As TextBox
    Private txtConfirmPassword As TextBox
    Private lblError As Label
    Private connectionString As String = "Server=.\SQLEXPRESS;Database=Inventory;Integrated Security=True;TrustServerCertificate=True;"

    Public Sub New()
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Change Password"
        Me.Size = New Size(520, 440)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        ModernTheme.EnableVerticalScroll(Me, 480)
        InitializeComponents()
    End Sub

    Private Sub InitializeComponents()
        Dim title As New Label With {
            .Text = "Change password",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(30, 24),
            .AutoSize = True
        }
        Me.Controls.Add(title)

        Dim panel As New Panel With {
            .Location = New Point(30, 70),
            .Size = New Size(440, 285),
            .BackColor = Color.White
        }
        Me.Controls.Add(panel)

        Dim lblCurrentUser As New Label With {
            .Text = "Username",
            .Location = New Point(20, 24),
            .Size = New Size(160, 24),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblCurrentUser)

        txtCurrentUsername = ModernTheme.MakeTextBox()
        txtCurrentUsername.Location = New Point(180, 20)
        txtCurrentUsername.Width = 220
        panel.Controls.Add(txtCurrentUsername)

        Dim lblCurrentPass As New Label With {
            .Text = "Current password",
            .Location = New Point(20, 74),
            .Size = New Size(160, 24),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblCurrentPass)

        txtCurrentPassword = ModernTheme.MakeTextBox()
        txtCurrentPassword.Location = New Point(180, 70)
        txtCurrentPassword.Width = 220
        txtCurrentPassword.PasswordChar = "*"c
        panel.Controls.Add(txtCurrentPassword)

        Dim lblNewPass As New Label With {
            .Text = "New password",
            .Location = New Point(20, 124),
            .Size = New Size(160, 24),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblNewPass)

        txtNewPassword = ModernTheme.MakeTextBox()
        txtNewPassword.Location = New Point(180, 120)
        txtNewPassword.Width = 220
        txtNewPassword.PasswordChar = "*"c
        panel.Controls.Add(txtNewPassword)

        Dim lblConfirmPass As New Label With {
            .Text = "Confirm password",
            .Location = New Point(20, 174),
            .Size = New Size(160, 24),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblConfirmPass)

        txtConfirmPassword = ModernTheme.MakeTextBox()
        txtConfirmPassword.Location = New Point(180, 170)
        txtConfirmPassword.Width = 220
        txtConfirmPassword.PasswordChar = "*"c
        panel.Controls.Add(txtConfirmPassword)

        lblError = New Label With {
            .Text = "",
            .ForeColor = ModernTheme.Danger,
            .Location = New Point(20, 214),
            .Size = New Size(380, 22),
            .Font = New Font("Segoe UI", 9.5F)
        }
        panel.Controls.Add(lblError)

        Dim btnSave As Button = ModernTheme.MakePrimaryButton("Save")
        btnSave.Location = New Point(180, 240)
        btnSave.Width = 120
        AddHandler btnSave.Click, AddressOf btnSave_Click
        panel.Controls.Add(btnSave)

        Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("Cancel")
        btnCancel.Location = New Point(310, 240)
        btnCancel.Width = 110
        btnCancel.ForeColor = ModernTheme.Danger
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        panel.Controls.Add(btnCancel)
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        lblError.Text = ""
        If String.IsNullOrWhiteSpace(txtCurrentUsername.Text) Then
            lblError.Text = "Username is required"
            txtCurrentUsername.Focus()
            Return
        End If
        If String.IsNullOrWhiteSpace(txtCurrentPassword.Text) Then
            lblError.Text = "Current password is required"
            txtCurrentPassword.Focus()
            Return
        End If
        If String.IsNullOrWhiteSpace(txtNewPassword.Text) Then
            lblError.Text = "New password is required"
            txtNewPassword.Focus()
            Return
        End If
        If txtNewPassword.Text.Length < 6 Then
            lblError.Text = "New password must be at least 6 characters"
            txtNewPassword.Focus()
            Return
        End If
        If txtNewPassword.Text <> txtConfirmPassword.Text Then
            lblError.Text = "New passwords do not match"
            txtNewPassword.Focus()
            Return
        End If

        Try
            Dim newPasswordHash As String = SecurityService.HashPassword(txtNewPassword.Text)
            Dim query As String = "SELECT UserID, PasswordHash FROM mn.UUser WHERE Username = @username"

            Using connection As New SqlConnection(connectionString)
                connection.Open()
                Using cmd As New SqlCommand(query, connection)
                    cmd.Parameters.AddWithValue("@username", txtCurrentUsername.Text.Trim())
                    Using reader = cmd.ExecuteReader()
                        If Not reader.Read() Then
                            lblError.Text = "Current username or password is incorrect"
                            Return
                        End If
                        Dim userID = Convert.ToInt32(reader("UserID"))
                        Dim storedHash = reader("PasswordHash").ToString()
                        Dim valid = SecurityService.VerifyPassword(txtCurrentPassword.Text, storedHash)
                        If Not valid AndAlso SecurityService.IsLegacyHash(storedHash) Then valid = SecurityService.VerifyLegacySha256(txtCurrentPassword.Text, storedHash)
                        If Not valid Then
                            lblError.Text = "Current username or password is incorrect"
                            Return
                        End If
                        reader.Close()
                        Using update As New SqlCommand("UPDATE mn.UUser SET PasswordHash = @newPassword WHERE UserID = @userID", connection)
                            update.Parameters.AddWithValue("@newPassword", newPasswordHash)
                            update.Parameters.AddWithValue("@userID", userID)
                            update.ExecuteNonQuery()
                        End Using
                        MessageBox.Show("Password changed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                    End Using
                End Using
                connection.Close()
            End Using
        Catch ex As Exception
            lblError.Text = "Error: " & ex.Message
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    ' Legacy hash function removed, PBKDF2 will be used instead
    ' Private Function HashPassword(password As String) As String
    '     Using sha256 As Security.Cryptography.SHA256 = Security.Cryptography.SHA256.Create()
    '         Dim bytes As Byte() = Encoding.UTF8.GetBytes(password)
    '         Dim hash As Byte() = sha256.ComputeHash(bytes)
    '         Return Convert.ToBase64String(hash)
    '     End Using
    ' End Function
End Class
