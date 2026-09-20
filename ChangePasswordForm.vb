Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Text

Public Class ChangePasswordForm
    Private txtCurrentUsername As TextBox
    Private txtCurrentPassword As TextBox
    Private txtNewPassword As TextBox
    Private txtConfirmPassword As TextBox
    Private lblError As Label

    ' Database connection string - CORRECTED
    Private connectionString As String = "Server=DESKTOP-KGESGJQ\NATNAEL;Database=Inventory;Integrated Security=True;"

    Public Sub New()
        ' Form setup
        Me.Text = "Change Password"
        Me.Size = New Size(400, 300)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False

        InitializeComponents()
    End Sub

    Private Sub InitializeComponents()
        ' Title
        Dim lblTitle As New Label With {
            .Text = "CHANGE PASSWORD",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Location = New Point(20, 20),
            .Size = New Size(360, 25),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' Current Username
        Dim lblCurrentUser As New Label With {
            .Text = "Username:",
            .Location = New Point(50, 60),
            .Size = New Size(100, 25)
        }

        txtCurrentUsername = New TextBox With {
            .Location = New Point(160, 60),
            .Size = New Size(180, 25),
            .Font = New Font("Segoe UI", 10)
        }

        ' Current Password
        Dim lblCurrentPass As New Label With {
            .Text = "Current Password:",
            .Location = New Point(50, 95),
            .Size = New Size(100, 25)
        }

        txtCurrentPassword = New TextBox With {
            .Location = New Point(160, 95),
            .Size = New Size(180, 25),
            .PasswordChar = "*"c,
            .Font = New Font("Segoe UI", 10)
        }

        ' New Password
        Dim lblNewPass As New Label With {
            .Text = "New Password:",
            .Location = New Point(50, 130),
            .Size = New Size(100, 25)
        }

        txtNewPassword = New TextBox With {
            .Location = New Point(160, 130),
            .Size = New Size(180, 25),
            .PasswordChar = "*"c,
            .Font = New Font("Segoe UI", 10)
        }

        ' Confirm Password
        Dim lblConfirmPass As New Label With {
            .Text = "Confirm Password:",
            .Location = New Point(50, 165),
            .Size = New Size(100, 25)
        }

        txtConfirmPassword = New TextBox With {
            .Location = New Point(160, 165),
            .Size = New Size(180, 25),
            .PasswordChar = "*"c,
            .Font = New Font("Segoe UI", 10)
        }

        ' Error label
        lblError = New Label With {
            .Text = "",
            .ForeColor = Color.Red,
            .Location = New Point(50, 200),
            .Size = New Size(300, 25)
        }

        ' Save button
        Dim btnSave As New Button With {
            .Text = "Save",
            .Location = New Point(120, 235),
            .Size = New Size(80, 30),
            .Font = New Font("Segoe UI", 9)
        }

        ' Cancel button
        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Location = New Point(220, 235),
            .Size = New Size(80, 30),
            .Font = New Font("Segoe UI", 9)
        }

        ' Add controls
        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblCurrentUser)
        Me.Controls.Add(txtCurrentUsername)
        Me.Controls.Add(lblCurrentPass)
        Me.Controls.Add(txtCurrentPassword)
        Me.Controls.Add(lblNewPass)
        Me.Controls.Add(txtNewPassword)
        Me.Controls.Add(lblConfirmPass)
        Me.Controls.Add(txtConfirmPassword)
        Me.Controls.Add(lblError)
        Me.Controls.Add(btnSave)
        Me.Controls.Add(btnCancel)

        ' Event handlers
        AddHandler btnSave.Click, AddressOf btnSave_Click
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        lblError.Text = ""

        ' Validate inputs
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
            ' Hash passwords
            Dim currentPasswordHash As String = HashPassword(txtCurrentPassword.Text)
            Dim newPasswordHash As String = HashPassword(txtNewPassword.Text)

            ' Create SQL query - CORRECTED FOR mn.Users
            Dim query As String = "UPDATE mn.Users SET PasswordHash = @newPassword WHERE Username = @username AND PasswordHash = @currentPassword"

            Using connection As New SqlConnection(connectionString)
                connection.Open()

                Using cmd As New SqlCommand(query, connection)
                    cmd.Parameters.AddWithValue("@newPassword", newPasswordHash)
                    cmd.Parameters.AddWithValue("@username", txtCurrentUsername.Text.Trim())
                    cmd.Parameters.AddWithValue("@currentPassword", currentPasswordHash)

                    Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                    If rowsAffected > 0 Then
                        MessageBox.Show("Password changed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                    Else
                        lblError.Text = "Current username or password is incorrect"
                    End If
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

    Private Function HashPassword(password As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(password)
            Dim hash As Byte() = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function
End Class