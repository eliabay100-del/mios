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
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Login - Inventory System"
        Me.Size = New Size(450, 320)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog

        ' Create card panel for consistent layout
        cardPanel = ModernTheme.CreateCardPanel()
        cardPanel.Location = New Point(ModernTheme.FormPadding, ModernTheme.FormPadding)
        cardPanel.Size = New Size(Me.ClientSize.Width - (2 * ModernTheme.FormPadding), 220)

        ' Initialize controls with consistent spacing
        lblUsername = New Label With {
            .Text = "Username:",
            .Location = New Point(0, 0),
            .Size = New Size(100, ModernTheme.FieldSpacing),
            .ForeColor = ModernTheme.PrimaryText
        }

        txtUsername = ModernTheme.MakeTextBox()
        txtUsername.Location = New Point(0, ModernTheme.FieldSpacing + ModernTheme.LabelToInputGap)
        txtUsername.Size = New Size(cardPanel.ClientSize.Width - ModernTheme.CardPadding * 2, 42)

        lblPassword = New Label With {
            .Text = "Password:",
            .Location = New Point(0, ModernTheme.FieldSpacing * 2 + ModernTheme.LabelToInputGap + 10),
            .Size = New Size(100, ModernTheme.FieldSpacing),
            .ForeColor = ModernTheme.PrimaryText
        }

        txtPassword = ModernTheme.MakeTextBox()
        txtPassword.Location = New Point(0, ModernTheme.FieldSpacing * 3 + ModernTheme.LabelToInputGap * 2 + 10)
        txtPassword.Size = New Size(cardPanel.ClientSize.Width - ModernTheme.CardPadding * 2, 42)
        txtPassword.PasswordChar = "*"c

        lblError = New Label With {
            .ForeColor = ModernTheme.Danger,
            .Location = New Point(0, ModernTheme.FieldSpacing * 4 + ModernTheme.LabelToInputGap * 3 + 20),
            .Size = New Size(cardPanel.ClientSize.Width - ModernTheme.CardPadding * 2, 20)
        }

        btnLogin = ModernTheme.MakePrimaryButton("Login")
        btnLogin.Location = New Point(0, ModernTheme.FieldSpacing * 5 + ModernTheme.LabelToInputGap * 4 + 30)
        btnLogin.Size = New Size(cardPanel.ClientSize.Width - ModernTheme.CardPadding * 2, 40)

        ' Add controls to card panel
        cardPanel.Controls.Add(lblUsername)
        cardPanel.Controls.Add(txtUsername)
        cardPanel.Controls.Add(lblPassword)
        cardPanel.Controls.Add(txtPassword)
        cardPanel.Controls.Add(lblError)
        cardPanel.Controls.Add(btnLogin)

        ' Add card panel to form
        Me.Controls.Add(cardPanel)

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
