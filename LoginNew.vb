Imports System.Data.SqlClient
Imports System.Windows.Forms
Imports System.Drawing

Public Class LoginNew
    Private connectionString As String = "Server=.\SQLEXPRESS;Database=Inventory;Integrated Security=True;TrustServerCertificate=True;"

    ' Controls
    Private lblUsername As Label
    Private lblPassword As Label
    Private txtUsername As TextBox
    Private txtPassword As TextBox
    Private btnLogin As Button
    Private lblError As Label
    Private cardPanel As Panel

    ' Properties to hold login info
    Public Property LoginSuccessful As Boolean = False
    Public Property UserID As Integer = 0
    Public Property Username As String = ""

    Public Sub New()
        ' Form settings
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(400, 520)
        Me.MinimumSize = New Size(400, 520)
        
        ' Apply gradient background
        Dim gradientBrush As New Drawing2D.LinearGradientBrush(
            Me.ClientRectangle,
            Color.FromArgb(240, 242, 245),
            Color.FromArgb(225, 228, 235),
            Drawing2D.LinearGradientMode.Vertical)
        Me.BackgroundImage = gradientBrush
        Me.BackgroundImageLayout = ImageLayout.Stretch

        ' Create card panel for consistent layout
        cardPanel = ModernTheme.CreateCardPanel()
        cardPanel.Location = New Point(50, 90)
        cardPanel.Size = New Size(300, 360)
        cardPanel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(cardPanel)

        ' Title label
        Dim lblTitle As New Label With {
            .Text = "Welcome Back",
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(30, 30, 30),
            .AutoSize = True,
            .Location = New Point((cardPanel.Width - 130) \ 2, 25),
            .BackColor = Color.Transparent,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        cardPanel.Controls.Add(lblTitle)
        
        ' Subtitle label
        Dim lblSubtitle As New Label With {
            .Text = "Please sign in to continue",
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(100, 100, 100),
            .AutoSize = True,
            .Location = New Point((cardPanel.Width - 150) \ 2, 58),
            .BackColor = Color.Transparent,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        cardPanel.Controls.Add(lblSubtitle)

        ' Username label
        lblUsername = New Label With {
            .Text = "Username",
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .AutoSize = False,
            .Size = New Size(260, 24),
            .Location = New Point(20, 100),
            .BackColor = ModernTheme.Surface
        }
        cardPanel.Controls.Add(lblUsername)
        
        ' Username textbox
        txtUsername = New TextBox With {
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Regular),
            .Size = New Size(260, 42),
            .Location = New Point(20, 128),
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.White,
            .ForeColor = Color.FromArgb(30, 30, 30)
        }
        cardPanel.Controls.Add(txtUsername)
        
        ' Password label
        lblPassword = New Label With {
            .Text = "Password",
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .AutoSize = False,
            .Size = New Size(260, 24),
            .Location = New Point(20, 182),
            .BackColor = ModernTheme.Surface
        }
        cardPanel.Controls.Add(lblPassword)
        
        ' Password textbox
        txtPassword = New TextBox With {
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Regular),
            .Size = New Size(260, 42),
            .Location = New Point(20, 210),
            .BorderStyle = BorderStyle.FixedSingle,
            .PasswordChar = "*"c,
            .BackColor = Color.White,
            .ForeColor = Color.FromArgb(30, 30, 30)
        }
        cardPanel.Controls.Add(txtPassword)
        
        ' Error label
        lblError = New Label With {
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(220, 50, 50),
            .AutoSize = False,
            .Size = New Size(260, 22),
            .Location = New Point(20, 264),
            .BackColor = ModernTheme.Surface,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        cardPanel.Controls.Add(lblError)
        
        ' Login button
        btnLogin = New Button With {
            .Text = "Sign In",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .Size = New Size(260, 48),
            .Location = New Point(20, 295),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(25, 118, 210),
            .ForeColor = Color.White,
            .Cursor = Cursors.Hand
        }
        btnLogin.FlatAppearance.BorderSize = 0
        btnLogin.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 101, 192)
        btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 136, 229)
        ModernTheme.ApplyRoundButton(btnLogin)
        cardPanel.Controls.Add(btnLogin)

        ' Event handlers
        AddHandler btnLogin.Click, AddressOf btnLogin_Click
        AddHandler txtPassword.KeyPress, AddressOf txtPassword_KeyPress
        AddHandler Me.Load, AddressOf LoginNew_Load
        AddHandler btnLogin.Resize, Sub(s, e) ModernTheme.ApplyRoundButton(CType(s, Button))

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
        Try
            Using con As New SqlConnection(connectionString)
                con.Open()
                Using cmd As New SqlCommand("SELECT u.UserID, u.Username, u.PasswordHash, ISNULL(r.RoleName, 'Storekeeper') AS RoleName FROM mn.UUser u LEFT JOIN mn.UserRole ur ON ur.UserID = u.UserID LEFT JOIN mn.Role r ON r.RoleID = ur.RoleID WHERE u.Username = @u", con)
                    cmd.Parameters.AddWithValue("@u", usernameInput)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Dim storedHash = reader("PasswordHash").ToString()
                            Dim passwordValid As Boolean = SecurityService.VerifyPassword(txtPassword.Text, storedHash)
                            If Not passwordValid AndAlso SecurityService.IsLegacyHash(storedHash) Then
                                passwordValid = SecurityService.VerifyLegacySha256(txtPassword.Text, storedHash)
                                If passwordValid Then MigratePasswordHash(UserID, txtPassword.Text)
                            End If
                            If passwordValid Then
                                ' Login success
                                LoginSuccessful = True
                                UserID = Convert.ToInt32(reader("UserID"))
                                Username = reader("Username").ToString()
                                SecurityContext.CurrentUserID = UserID
                                SecurityContext.CurrentUsername = Username
                                SecurityContext.CurrentRole = reader("RoleName").ToString()

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

    Private Sub MigratePasswordHash(userID As Integer, password As String)
        Using connection As New SqlConnection(connectionString)
            connection.Open()
            Using cmd As New SqlCommand("UPDATE mn.UUser SET PasswordHash = @hash WHERE UserID = @userID", connection)
                cmd.Parameters.AddWithValue("@hash", SecurityService.HashPassword(password))
                cmd.Parameters.AddWithValue("@userID", userID)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
End Class
