Imports System.Data.SqlClient
Imports System.Windows.Forms
Imports System.Drawing

Public Class LoginNew
    Private connectionString As String = DBModule.ConnString

    ' Controls
    Private WithEvents txtUsername As New TextBox With {.Location = New Point(138, 172), .Size = New Size(240, 25), .TabIndex = 0}
    Private WithEvents txtPassword As New TextBox With {.Location = New Point(138, 214), .Size = New Size(240, 25), .PasswordChar = "*"c, .TabIndex = 1}
    Private WithEvents btnLogin As New Button With {.Text = "Sign in", .Location = New Point(138, 282), .Size = New Size(240, 44), .TabIndex = 2}
    Private WithEvents lblError As New Label With {.ForeColor = Color.Red, .Location = New Point(48, 252), .Size = New Size(330, 20)}

    ' Properties to hold login info
    Public Property LoginSuccessful As Boolean = False
    Public Property UserID As Integer = 0
    Public Property Username As String = ""

    ' Simple in-process brute-force protection.
    Private Const MaxFailedAttempts As Integer = 5
    Private ReadOnly LockoutDuration As TimeSpan = TimeSpan.FromSeconds(30)
    Private failedAttempts As Integer = 0
    Private lockoutUntil As DateTime = DateTime.MinValue

    Public Sub New()
        InitializeComponent()
        ' Form settings
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "MIOS Inventory Management"
        Me.Size = New Size(440, 400)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Logo and heading
        Dim logo As PictureBox = ModernTheme.MakeLogo(88)
        If logo IsNot Nothing Then
            logo.Location = New Point(173, 22)
            logo.BackColor = ModernTheme.Surface
            Me.Controls.Add(logo)
        End If

        Dim lblTitle As New Label With {
            .Text = "MIOS Inventory Management",
            .Location = New Point(0, 122),
            .Size = New Size(434, 30),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 15.0F, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText
        }
        Me.Controls.Add(lblTitle)

        ' Add controls
        Me.Controls.Add(New Label With {.Text = "Username:", .Location = New Point(48, 176), .Size = New Size(84, 25)})
        Me.Controls.Add(txtUsername)
        Me.Controls.Add(New Label With {.Text = "Password:", .Location = New Point(48, 218), .Size = New Size(84, 25)})
        Me.Controls.Add(txtPassword)
        Me.Controls.Add(lblError)
        Me.Controls.Add(btnLogin)

        ' Enter submits from anywhere on the form, and TAB moves username -> password -> Sign in.
        Me.AcceptButton = btnLogin

        ' Style the sign-in button
        btnLogin.BackColor = ModernTheme.Accent
        btnLogin.ForeColor = Color.White
        btnLogin.FlatStyle = FlatStyle.Flat
        btnLogin.FlatAppearance.BorderSize = 0
        btnLogin.UseVisualStyleBackColor = False
        btnLogin.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        btnLogin.Cursor = Cursors.Hand

        ' Event handlers
        AddHandler btnLogin.Click, AddressOf btnLogin_Click
        AddHandler txtPassword.KeyPress, AddressOf txtPassword_KeyPress
        AddHandler Me.Load, AddressOf LoginNew_Load

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

        If DateTime.Now < lockoutUntil Then
            Dim secondsLeft As Double = Math.Ceiling((lockoutUntil - DateTime.Now).TotalSeconds)
            lblError.Text = $"Too many failed attempts. Try again in {secondsLeft:0}s."
            Return
        End If

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
                            ' Capture the user id before verification so a legacy
                            ' hash is migrated against the correct row.
                            Dim authedUserID As Integer = Convert.ToInt32(reader("UserID"))
                            Dim storedHash = reader("PasswordHash").ToString()

                            Dim passwordValid As Boolean = SecurityService.VerifyPassword(txtPassword.Text, storedHash)
                            If Not passwordValid AndAlso SecurityService.IsLegacyHash(storedHash) Then
                                passwordValid = SecurityService.VerifyLegacySha256(txtPassword.Text, storedHash)
                                If passwordValid Then MigratePasswordHash(authedUserID, txtPassword.Text)
                            End If

                            If passwordValid Then
                                ' Login success
                                failedAttempts = 0
                                lockoutUntil = DateTime.MinValue
                                LoginSuccessful = True
                                UserID = authedUserID
                                Username = reader("Username").ToString()
                                SecurityContext.CurrentUserID = UserID
                                SecurityContext.CurrentUsername = Username
                                SecurityContext.CurrentRole = reader("RoleName").ToString()
                                SecurityContext.LoadPermissions()

                                ' Open Dashboard
                                Dim dashboard As New Dashboard()
                                dashboard.Username = Me.Username
                                dashboard.UserID = Me.UserID
                                Me.Hide()
                                dashboard.ShowDialog()
                                SecurityContext.Clear()
                                Me.Close()
                            Else
                                RegisterFailedAttempt()
                                lblError.Text = "Invalid username or password"
                            End If
                        Else
                            RegisterFailedAttempt()
                            lblError.Text = "Invalid username or password"
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            AppLogger.Error("Login failed", ex)
            MessageBox.Show("Unable to sign in. Please check that the database is available and try again.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub RegisterFailedAttempt()
        failedAttempts += 1
        If failedAttempts >= MaxFailedAttempts Then
            lockoutUntil = DateTime.Now.Add(LockoutDuration)
            failedAttempts = 0
        End If
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
