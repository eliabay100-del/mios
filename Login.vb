Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Text
Imports System.Drawing
Imports System.Windows.Forms

Public Class Login
    ' UI Controls
    Private WithEvents txtUsername As TextBox
    Private WithEvents txtPassword As TextBox
    Private WithEvents btnLogin As Button
    Private WithEvents btnChangeCredentials As Button
    Private WithEvents lblError As Label

    ' Database connection string
    Private connectionString As String = "Server=.\SQLEXPRESS;Database=Inventory;Integrated Security=True;TrustServerCertificate=True;"

    Public Property LoginSuccessful As Boolean = False
    Public Property Username As String = ""
    Public Property UserID As Integer = 0

    Public Sub New()
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Login - Inventory System"
        Me.Size = New Size(1000, 620)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.None
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ControlBox = False

        InitializeComponent()
        InitializeComponents()
    End Sub

    Private Sub InitializeComponents()
        Dim leftPanel As New Panel With {
            .Dock = DockStyle.Left,
            .Width = 470,
            .BackColor = Color.FromArgb(18, 29, 37)
        }

        Dim heroOverlay As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(18, 0, 0, 0)
        }

        Dim heroGradient As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(25, 33, 42)
        }
        leftPanel.Controls.Add(heroGradient)
        heroGradient.Controls.Add(heroOverlay)

        Dim brand As New Label With {
            .Text = "MIOS",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .Location = New Point(32, 24),
            .AutoSize = True
        }
        heroOverlay.Controls.Add(brand)

        Dim title As New Label With {
            .Text = "Inventory system",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 32, FontStyle.Bold),
            .Location = New Point(32, 140),
            .AutoSize = True
        }
        heroOverlay.Controls.Add(title)

        Dim subtitle As New Label With {
            .Text = "Track stock, suppliers, sales, and inventory movement in a clean personal workspace.",
            .ForeColor = Color.FromArgb(230, 230, 230),
            .Font = New Font("Segoe UI", 12.0F),
            .Location = New Point(32, 205),
            .Size = New Size(340, 80)
        }
        heroOverlay.Controls.Add(subtitle)

        Dim rightPanel As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = ModernTheme.PageBackground
        }

        Dim card As New Panel With {
            .Size = New Size(390, 380),
            .Location = New Point(90, 110),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.None
        }

        Dim titleLabel As New Label With {
            .Text = "Welcome back",
            .ForeColor = ModernTheme.PrimaryText,
            .Font = New Font("Segoe UI", 28, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(30, 28)
        }
        card.Controls.Add(titleLabel)

        Dim subLabel As New Label With {
            .Text = "Sign in to continue",
            .ForeColor = ModernTheme.MutedText,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Regular),
            .AutoSize = True,
            .Location = New Point(30, 74)
        }
        card.Controls.Add(subLabel)

        Dim lblUser As New Label With {
            .Text = "Username",
            .ForeColor = ModernTheme.MutedText,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .Location = New Point(30, 118),
            .AutoSize = True
        }
        card.Controls.Add(lblUser)

        txtUsername = ModernTheme.MakeTextBox()
        txtUsername.Location = New Point(30, 142)
        txtUsername.Width = 330
        card.Controls.Add(txtUsername)

        Dim lblPass As New Label With {
            .Text = "Password",
            .ForeColor = ModernTheme.MutedText,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .Location = New Point(30, 196),
            .AutoSize = True
        }
        card.Controls.Add(lblPass)

        txtPassword = ModernTheme.MakeTextBox()
        txtPassword.Location = New Point(30, 220)
        txtPassword.Width = 330
        txtPassword.PasswordChar = "*"c
        card.Controls.Add(txtPassword)

        lblError = New Label With {
            .Text = "",
            .ForeColor = ModernTheme.Danger,
            .Font = New Font("Segoe UI", 9.5F),
            .Location = New Point(30, 270),
            .Size = New Size(330, 20)
        }
        card.Controls.Add(lblError)

        btnLogin = ModernTheme.MakePrimaryButton("Sign in")
        btnLogin.Location = New Point(30, 300)
        btnLogin.Width = 330
        btnLogin.Height = 42
        card.Controls.Add(btnLogin)

        btnChangeCredentials = ModernTheme.MakeSecondaryButton("Change password")
        btnChangeCredentials.Location = New Point(30, 350)
        btnChangeCredentials.Width = 330
        btnChangeCredentials.Height = 38
        card.Controls.Add(btnChangeCredentials)

        rightPanel.Controls.Add(card)
        Me.Controls.Add(leftPanel)
        Me.Controls.Add(rightPanel)

        AddHandler btnLogin.Click, AddressOf btnLogin_Click
        AddHandler btnChangeCredentials.Click, AddressOf btnChangeCredentials_Click
        AddHandler txtPassword.KeyPress, AddressOf txtPassword_KeyPress

        txtUsername.Focus()
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

    Private Sub btnChangeCredentials_Click(sender As Object, e As EventArgs)
        Dim changeForm As New ChangePasswordForm()
        changeForm.ShowDialog()
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

        Try
            Dim currentUsername As String = txtUsername.Text.Trim().ToLower()
            Dim hashedPassword As String = HashPassword(txtPassword.Text)
            Dim query As String = "SELECT UserID, Username, PasswordHash FROM mn.Users WHERE Username = @username"

            Using connection As New SqlConnection(connectionString)
                connection.Open()

                Using cmd As New SqlCommand(query, connection)
                    cmd.Parameters.AddWithValue("@username", currentUsername)

                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Dim storedHash As String = reader("PasswordHash").ToString().Trim()
                            If storedHash.ToUpper() = hashedPassword.ToUpper() Then
                                LoginSuccessful = True
                                UserID = Convert.ToInt32(reader("UserID"))
                                Username = reader("Username").ToString()

                                Me.Hide()
                                Dim dashboard As New Dashboard()
                                dashboard.UserID = UserID
                                dashboard.Username = Username
                                dashboard.Show()
                                Me.Close()
                            Else
                                lblError.Text = "Invalid password"
                                txtPassword.SelectAll()
                                txtPassword.Focus()
                            End If
                        Else
                            lblError.Text = "Invalid username"
                            txtUsername.SelectAll()
                            txtUsername.Focus()
                        End If
                    End Using
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show("System Error: " & ex.Message, "Error")
            lblError.Text = "Login error"
        End Try
    End Sub

    Private Function HashPassword(password As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(password)
            Dim hash As Byte() = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function
End Class
