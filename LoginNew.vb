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
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog

        ' Create card panel for consistent layout
        cardPanel = ModernTheme.CreateCardPanel()
        cardPanel.Location = New Point(ModernTheme.FormPadding, ModernTheme.FormPadding)
        cardPanel.Width = 320 ' Fixed width matching original design (400 - 2*FormPadding)

        ' Initialize controls with consistent spacing matching original design
        Dim yPos As Integer = 0
        
        lblUsername = New Label With {
            .Text = "Username:",
            .Location = New Point(0, yPos),
            .AutoSize = True,
            .BackColor = ModernTheme.Surface,
            .ForeColor = ModernTheme.PrimaryText
        }
        cardPanel.Controls.Add(lblUsername)
        
        yPos = lblUsername.Bottom + ModernTheme.LabelToInputGap
        txtUsername = ModernTheme.MakeTextBox()
        txtUsername.Location = New Point(0, yPos)
        txtUsername.Size = New Size(200, 25) ' Original size
        cardPanel.Controls.Add(txtUsername)
        
        yPos = txtUsername.Bottom + ModernTheme.FieldSpacing
        lblPassword = New Label With {
            .Text = "Password:",
            .Location = New Point(0, yPos),
            .AutoSize = True,
            .BackColor = ModernTheme.Surface,
            .ForeColor = ModernTheme.PrimaryText
        }
        cardPanel.Controls.Add(lblPassword)
        
        yPos = lblPassword.Bottom + ModernTheme.LabelToInputGap
        txtPassword = ModernTheme.MakeTextBox()
        txtPassword.Location = New Point(0, yPos)
        txtPassword.Size = New Size(200, 25) ' Original size
        txtPassword.PasswordChar = "*"c
        cardPanel.Controls.Add(txtPassword)
        
        yPos = txtPassword.Bottom + ModernTheme.FieldSpacing
        lblError = New Label With {
            .ForeColor = ModernTheme.Danger,
            .Location = New Point(0, yPos),
            .AutoSize = True,
            .BackColor = ModernTheme.Surface,
            .MaximumSize = New Size(300, 0),
            .Size = New Size(300, 20) ' Original size
        }
        cardPanel.Controls.Add(lblError)
        
        yPos = lblError.Bottom + ModernTheme.FieldSpacing
        btnLogin = ModernTheme.MakePrimaryButton("Login")
        btnLogin.Location = New Point(0, yPos)
        btnLogin.Size = New Size(80, 30) ' Original size
        ModernTheme.ApplyRoundButton(btnLogin)
        cardPanel.Controls.Add(btnLogin)

        ' Size cardPanel and form to fit content exactly
        cardPanel.Height = yPos + btnLogin.Height + ModernTheme.CardPadding
        Me.ClientSize = New Size(cardPanel.Width + (2 * ModernTheme.FormPadding), cardPanel.Height + (2 * ModernTheme.FormPadding))

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
