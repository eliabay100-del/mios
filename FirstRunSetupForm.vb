Imports System.Drawing
Imports System.Windows.Forms

Public Class FirstRunSetupForm
    Inherits Form

    Private ReadOnly txtUsername As New TextBox()
    Private ReadOnly txtPassword As New TextBox()
    Private ReadOnly txtConfirmPassword As New TextBox()
    Private ReadOnly lblError As New Label()

    Public Sub New()
        Me.Text = "Create MIOS user"
        Me.Size = New Size(460, 330)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        Dim title As New Label With {
            .Text = "Create your first user",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .Location = New Point(30, 24),
            .AutoSize = True
        }
        Me.Controls.Add(title)

        AddField("Username", txtUsername, 82, False)
        AddField("Password", txtPassword, 132, True)
        AddField("Confirm password", txtConfirmPassword, 182, True)

        lblError.ForeColor = Color.Firebrick
        lblError.Location = New Point(30, 220)
        lblError.Size = New Size(390, 24)
        Me.Controls.Add(lblError)

        Dim btnCreate As New Button With {
            .Text = "Create user",
            .Location = New Point(270, 255),
            .Size = New Size(140, 35),
            .DialogResult = DialogResult.None
        }
        AddHandler btnCreate.Click, AddressOf CreateUser_Click
        Me.Controls.Add(btnCreate)
        Me.AcceptButton = btnCreate
        txtUsername.Focus()
    End Sub

    Private Sub AddField(labelText As String, textBox As TextBox, top As Integer, password As Boolean)
        Me.Controls.Add(New Label With {.Text = labelText, .Location = New Point(30, top + 4), .Size = New Size(130, 24)})
        textBox.Location = New Point(170, top)
        textBox.Size = New Size(240, 25)
        If password Then textBox.PasswordChar = "*"c
        Me.Controls.Add(textBox)
    End Sub

    Private Sub CreateUser_Click(sender As Object, e As EventArgs)
        lblError.Text = ""
        Dim username = txtUsername.Text.Trim()

        If username.Length = 0 Then
            lblError.Text = "Username is required."
            Return
        End If
        If txtPassword.Text.Length < 6 Then
            lblError.Text = "Password must be at least 6 characters."
            Return
        End If
        If txtPassword.Text <> txtConfirmPassword.Text Then
            lblError.Text = "Passwords do not match."
            Return
        End If

        Try
            UserSetup.CreateUser(username, txtPassword.Text)
            MessageBox.Show("User created successfully. You can now sign in.", "Setup complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            lblError.Text = "Unable to create user: " & ex.Message
        End Try
    End Sub
End Class