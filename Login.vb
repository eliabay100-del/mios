Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Text
Imports System.IO

Public Class Login
    ' UI Controls
    Private WithEvents txtUsername As TextBox
    Private WithEvents txtPassword As TextBox
    Private WithEvents btnLogin As Button
    Private WithEvents btnCancel As Button
    Private WithEvents btnChangeCredentials As Button
    Private WithEvents lblError As Label

    ' Database connection string
    Private connectionString As String = "Server=DESKTOP-KGESGJQ\NATNAEL;Database=Inventory;Integrated Security=True;"

    ' Login result
    Public Property LoginSuccessful As Boolean = False
    Public Property Username As String = ""
    Public Property UserID As Integer = 0

    Public Sub New()
        ' Form setup
        Me.Text = "Login - Inventory System"
        Me.Size = New Size(400, 250)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        InitializeComponents()
    End Sub

    Private Sub InitializeComponents()
        ' Title
        Dim lblTitle As New Label With {
            .Text = "INVENTORY SYSTEM",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 20),
            .Size = New Size(360, 30),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' Username
        Dim lblUser As New Label With {
            .Text = "Username:",
            .Location = New Point(50, 70),
            .Size = New Size(80, 25)
        }

        txtUsername = New TextBox With {
            .Location = New Point(130, 70),
            .Size = New Size(200, 25),
            .Font = New Font("Segoe UI", 10)
        }

        ' Password
        Dim lblPass As New Label With {
            .Text = "Password:",
            .Location = New Point(50, 110),
            .Size = New Size(80, 25)
        }

        txtPassword = New TextBox With {
            .Location = New Point(130, 110),
            .Size = New Size(200, 25),
            .PasswordChar = "*"c,
            .Font = New Font("Segoe UI", 10)
        }

        ' Error label
        lblError = New Label With {
            .Text = "",
            .ForeColor = Color.Red,
            .Location = New Point(50, 140),
            .Size = New Size(300, 20)
        }

        ' Login button
        btnLogin = New Button With {
            .Text = "Login",
            .Location = New Point(50, 170),
            .Size = New Size(80, 30),
            .Font = New Font("Segoe UI", 9)
        }

        ' Change Password button
        btnChangeCredentials = New Button With {
            .Text = "Change Password",
            .Location = New Point(140, 170),
            .Size = New Size(120, 30),
            .Font = New Font("Segoe UI", 9)
        }

        ' Cancel button
        btnCancel = New Button With {
            .Text = "Cancel",
            .Location = New Point(270, 170),
            .Size = New Size(80, 30),
            .Font = New Font("Segoe UI", 9)
        }

        ' Add controls to form
        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblUser)
        Me.Controls.Add(txtUsername)
        Me.Controls.Add(lblPass)
        Me.Controls.Add(txtPassword)
        Me.Controls.Add(lblError)
        Me.Controls.Add(btnLogin)
        Me.Controls.Add(btnChangeCredentials)
        Me.Controls.Add(btnCancel)

        ' Add event handlers
        AddHandler btnLogin.Click, AddressOf btnLogin_Click
        AddHandler btnChangeCredentials.Click, AddressOf btnChangeCredentials_Click
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        AddHandler txtPassword.KeyPress, AddressOf txtPassword_KeyPress

        txtUsername.Focus()
    End Sub

    Private Sub btnLogin_Click(sender As Object, e As EventArgs)
        'MessageBox.Show(HashPassword("nati"))
        AuthenticateUser()



    End Sub

    Private Sub txtPassword_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            AuthenticateUser()
            e.Handled = True
        End If
    End Sub

    Private Sub AuthenticateUser()
        ' Clear previous error messages
        lblError.Text = ""

        ' 1. Validate inputs are not empty
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
            ' 2. Prepare the username - use ToLower to stay consistent with your DB registration
            Dim currentUsername As String = txtUsername.Text.Trim().ToLower()

            ' 3. Generate the hash using the "Username + Password" method
            Dim hashedPassword As String =
    HashPassword(txtPassword.Text)


            ' 4. SQL Query to find the specific user
            Dim query As String = "SELECT UserID, Username, PasswordHash FROM mn.Users WHERE Username = @username"

            Using connection As New SqlConnection(connectionString)
                connection.Open()

                Using cmd As New SqlCommand(query, connection)
                    cmd.Parameters.AddWithValue("@username", currentUsername)

                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' 5. Get and clean the hash stored in the database
                            Dim storedHash As String = reader("PasswordHash").ToString().Trim()

                            ' 6. THE CRITICAL FIX: Force both strings to UpperCase for the comparison
                            ' This resolves the "44 vs 44" mismatch where characters look identical 
                            ' but have different casing.
                            If storedHash.ToUpper() = hashedPassword.ToUpper() Then
                                ' SUCCESS: User is authenticated
                                LoginSuccessful = True
                                UserID = Convert.ToInt32(reader("UserID"))
                                Username = reader("Username").ToString()

                                ' 7. Open the Dashboard and close the login form
                                Me.Hide()
                                Dim dashboard As New Dashboard()
                                dashboard.UserID = UserID
                                dashboard.Username = Username
                                dashboard.Show()
                                Me.Close()
                            Else
                                ' 8. Error handling for password mismatch
                                lblError.Text = "Invalid password"

                                ' Optional Debug box for your school report
                                MessageBox.Show("STILL NOT MATCHING!" & vbCrLf &
                                            "DB Hash: " & storedHash & vbCrLf &
                                            "Our Hash: " & hashedPassword & vbCrLf &
                                            "Lengths: " & storedHash.Length & " vs " & hashedPassword.Length,
                                            "Final Debug Check")

                                txtPassword.SelectAll()
                                txtPassword.Focus()
                            End If
                        Else
                            ' Username does not exist in the database
                            lblError.Text = "Invalid username"
                            txtUsername.SelectAll()
                            txtUsername.Focus()
                        End If
                    End Using
                End Using
                connection.Close()
            End Using

        Catch ex As Exception
            ' Catch any unexpected database or hashing errors
            MessageBox.Show("System Error: " & ex.Message, "Error")
            lblError.Text = "Login error"
        End Try
    End Sub
    Private Sub btnChangeCredentials_Click(sender As Object, e As EventArgs)
        Dim changeForm As New ChangePasswordForm()
        changeForm.ShowDialog()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Application.Exit()
    End Sub

    ' SHA256 hashing - Username + Password (matches your DB)
    Private Function HashPasswordWithUsername(username As String, password As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            ' Ensure combination matches the registration logic (Username then Password)
            Dim combinedString As String = username & password
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(combinedString)
            Dim hash As Byte() = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function

    ' Legacy method - keep if needed for other parts of the project
    Private Function HashPassword(password As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(password)
            Dim hash As Byte() = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function

    Private Sub Login_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    End Sub
End Class