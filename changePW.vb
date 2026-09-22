Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Text
Imports System.Drawing
Imports System.Windows.Forms

Public Class ChangePW
    Inherits Form

    ' Database connection
    Private connectionString As String = "Server=.\SQLEXPRESS;Database=Inventory;Integrated Security=True;TrustServerCertificate=True;"

    ' Controls
    Private lblCurrent As Label
    Private txtCurrent As TextBox
    Private lblNew As Label
    Private txtNew As TextBox
    Private lblConfirm As Label
    Private txtConfirm As TextBox
    Private btnChange As Button
    Private lblMessage As Label

    ' Logged-in username
    Public Property Username As String = ""

    Public Sub New()
        InitializeComponent()
        ' Form settings
        Me.Text = "Change Password"
        Me.Size = New Size(400, 300)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        InitializeControls()
    End Sub

    Private Sub InitializeControls()
        ' Current Password
        lblCurrent = New Label With {.Text = "Current Password:", .Location = New Point(30, 30), .Size = New Size(120, 25)}
        txtCurrent = New TextBox With {.Location = New Point(160, 30), .Size = New Size(180, 25), .PasswordChar = "*"c}

        ' New Password
        lblNew = New Label With {.Text = "New Password:", .Location = New Point(30, 70), .Size = New Size(120, 25)}
        txtNew = New TextBox With {.Location = New Point(160, 70), .Size = New Size(180, 25), .PasswordChar = "*"c}

        ' Confirm New Password
        lblConfirm = New Label With {.Text = "Confirm Password:", .Location = New Point(30, 110), .Size = New Size(120, 25)}
        txtConfirm = New TextBox With {.Location = New Point(160, 110), .Size = New Size(180, 25), .PasswordChar = "*"c}

        ' Change button
        btnChange = New Button With {.Text = "Change Password", .Location = New Point(120, 160), .Size = New Size(140, 30)}
        AddHandler btnChange.Click, AddressOf btnChange_Click

        ' Message label
        lblMessage = New Label With {.Text = "", .Location = New Point(30, 200), .Size = New Size(320, 40), .ForeColor = Color.Red}

        ' Add controls to form
        Me.Controls.Add(lblCurrent)
        Me.Controls.Add(txtCurrent)
        Me.Controls.Add(lblNew)
        Me.Controls.Add(txtNew)
        Me.Controls.Add(lblConfirm)
        Me.Controls.Add(txtConfirm)
        Me.Controls.Add(btnChange)
        Me.Controls.Add(lblMessage)
    End Sub

    Private Sub btnChange_Click(sender As Object, e As EventArgs)
        lblMessage.Text = ""
        lblMessage.ForeColor = Color.Red

        If String.IsNullOrWhiteSpace(txtCurrent.Text) OrElse String.IsNullOrWhiteSpace(txtNew.Text) OrElse String.IsNullOrWhiteSpace(txtConfirm.Text) Then
            lblMessage.Text = "All fields are required."
            Return
        End If

        If txtNew.Text <> txtConfirm.Text Then
            lblMessage.Text = "New passwords do not match."
            Return
        End If

        Try
            Using con As New SqlConnection(connectionString)
                con.Open()

                ' Verify current password
                Dim cmdCheck As New SqlCommand("SELECT PasswordHash FROM mn.UUser WHERE Username = @u", con)
                cmdCheck.Parameters.AddWithValue("@u", Username)
                Dim storedHash As String = cmdCheck.ExecuteScalar()?.ToString()

                If storedHash Is Nothing Then
                    lblMessage.Text = "User not found."
                    Return
                End If

                If storedHash <> HashPassword(txtCurrent.Text) Then
                    lblMessage.Text = "Current password is incorrect."
                    Return
                End If

                ' Update to new password
                Dim cmdUpdate As New SqlCommand("UPDATE mn.UUser SET PasswordHash = @p WHERE Username = @u", con)
                cmdUpdate.Parameters.AddWithValue("@p", HashPassword(txtNew.Text))
                cmdUpdate.Parameters.AddWithValue("@u", Username)
                cmdUpdate.ExecuteNonQuery()

                lblMessage.ForeColor = Color.Green
                lblMessage.Text = "Password changed successfully."

                txtCurrent.Clear()
                txtNew.Clear()
                txtConfirm.Clear()
            End Using
        Catch ex As Exception
            lblMessage.Text = "Error: " & ex.Message
        End Try
    End Sub

    Private Function HashPassword(password As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            Dim bytes = Encoding.UTF8.GetBytes(password)
            Dim hash = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function

End Class
