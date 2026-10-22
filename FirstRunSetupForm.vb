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
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.WindowState = FormWindowState.Maximized
        Me.AutoScroll = True
        ModernTheme.ApplyWindowTheme(Me)

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

        lblError.ForeColor = ModernTheme.Danger
        lblError.Location = New Point(30, 228)
        lblError.Size = New Size(450, 24)
        Me.Controls.Add(lblError)

        Dim btnCreate As Button = ModernTheme.MakePrimaryButton("Create user")
        btnCreate.Location = New Point(230, 268)
        btnCreate.Size = New Size(140, 35)
        btnCreate.DialogResult = DialogResult.None
        AddHandler btnCreate.Click, AddressOf CreateUser_Click
        Me.Controls.Add(btnCreate)
        Me.AcceptButton = btnCreate

        ' Without a way back the only escape was the window's close box, which
        ' left the user unsure whether the setup had done anything.
        Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("Cancel")
        btnCancel.Location = New Point(380, 268)
        btnCancel.Size = New Size(100, 35)
        btnCancel.DialogResult = DialogResult.Cancel
        Me.Controls.Add(btnCancel)
        Me.CancelButton = btnCancel

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
            AppLogger.Error("Failed to create first user", ex)
            lblError.Text = "Unable to create user: " & ex.Message
        End Try
    End Sub
End Class