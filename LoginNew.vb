Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing

Public Class LoginNew
    Private connectionString As String = "Server=.\SQLEXPRESS;Database=Inventory;Integrated Security=True;TrustServerCertificate=True;"

    ' Controls
    Private WithEvents txtUsername As New TextBox With {.Location = New Point(130, 70), .Size = New Size(200, 25)}
    Private WithEvents txtPassword As New TextBox With {.Location = New Point(130, 110), .Size = New Size(200, 25), .PasswordChar = "*"c}
    Private WithEvents btnLogin As New Button With {.Text = "Login", .Location = New Point(50, 170), .Size = New Size(80, 30)}
    Private WithEvents lblError As New Label With {.ForeColor = Color.Red, .Location = New Point(50, 140), .Size = New Size(300, 20)}

    ' Properties to hold login info
    Public Property LoginSuccessful As Boolean = False
    Public Property UserID As Integer = 0
    Public Property Username As String = ""

    Public Sub New()
        ' Form settings
        Me.Text = "Login - Inventory System"
        ModernTheme.ApplyWindowTheme(Me)
        Me.Size = New Size(520, 360)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog

        ' Add controls
        Me.Controls.Add(New Label With {.Text = "Username:", .ForeColor = ModernTheme.MutedText, .Location = New Point(70, 92), .Size = New Size(80, 25)})
        Me.Controls.Add(txtUsername)
        Me.Controls.Add(New Label With {.Text = "Password:", .ForeColor = ModernTheme.MutedText, .Location = New Point(70, 142), .Size = New Size(80, 25)})
        Me.Controls.Add(txtPassword)
        Me.Controls.Add(lblError)
        Me.Controls.Add(btnLogin)

        ' Event handlers
        AddHandler btnLogin.Click, AddressOf btnLogin_Click
        AddHandler txtPassword.KeyPress, AddressOf txtPassword_KeyPress
        AddHandler Me.Load, AddressOf LoginNew_Load

        txtUsername.Location = New Point(170, 88)
        txtUsername.Width = 270
        txtPassword.Location = New Point(170, 138)
        txtPassword.Width = 270
        btnLogin.Location = New Point(170, 228)
        btnLogin.Size = New Size(270, 42)
        btnLogin.BackColor = ModernTheme.Accent
        lblError.Location = New Point(170, 178)
        lblError.Size = New Size(270, 24)
        Me.Controls.Add(New Label With {.Text = "MIOS / INVENTORY", .Font = New Font("Segoe UI", 18, FontStyle.Bold), .ForeColor = ModernTheme.PrimaryText, .Location = New Point(70, 34), .AutoSize = True})
        Me.Controls.Add(New Label With {.Text = "Sign in to your operations workspace", .ForeColor = ModernTheme.MutedText, .Location = New Point(72, 62), .AutoSize = True})
        txtUsername.Focus()
    End Sub

    Private Sub LoginNew_Load(sender As Object, e As EventArgs)
        Try
            If Not UserSetup.HasUsers() Then
                Me.Hide()
                Using setup As New FirstRunSetupForm()
                    If setup.ShowDialog() <> DialogResult.OK Then
                        Me.Close()
                        Return
                    End If
                End Using
                Me.Show()
                txtUsername.Focus()
            End If
        Catch ex As Exception
            MessageBox.Show("Unable to check user setup: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub txtPassword_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            AuthenticateUser()
            e.Handled = True
        End If
    End Sub

    Private Sub btnLogin_Click(sender As Object, e As EventArgs)
        AuthenticateUser()
    End Sub

    Private Sub AuthenticateUser()
        lblError.Text = ""

        If String.IsNullOrWhiteSpace(txtUsername.Text) Then
            lblError.Text = "Username is required"
            txtUsername.Focus()
            Return
        End If

        If String.IsNullOrWhiteSpace(txtPassword.Text) Then
            lblError.Text = "Password is required"
            txtPassword.Focus()
            Return
        End If

        Dim usernameInput As String = txtUsername.Text.Trim()
        Dim passwordHash As String = HashPassword(txtPassword.Text)

        Try
            Using con As New SqlConnection(connectionString)
                con.Open()
                Using cmd As New SqlCommand("SELECT UserID, Username, PasswordHash FROM mn.UUser WHERE Username = @u", con)
                    cmd.Parameters.AddWithValue("@u", usernameInput)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Dim storedHash = reader("PasswordHash").ToString()
                            If storedHash = passwordHash Then
                                ' Login success
                                LoginSuccessful = True
                                UserID = Convert.ToInt32(reader("UserID"))
                                Username = reader("Username").ToString()

                                ' Open Dashboard
                                Dim dashboard As New Dashboard()
                                dashboard.Username = Me.Username
                                dashboard.UserID = Me.UserID
                                Me.Hide()
                                dashboard.ShowDialog()
                                Me.Close()
                            Else
                                lblError.Text = "Invalid password"
                            End If
                        Else
                            lblError.Text = "Invalid username"
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Login error: " & ex.Message)
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
