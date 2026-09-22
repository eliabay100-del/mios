Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Text
Imports System.Windows.Forms

Module UserSetup
    Private connectionString As String = "Server=.\SQLEXPRESS;Database=Inventory;Integrated Security=True;TrustServerCertificate=True;"

    Public Function HasUsers() As Boolean
        Using con As New SqlConnection(connectionString)
            con.Open()
            Using cmd As New SqlCommand("SELECT COUNT(*) FROM mn.UUser", con)
                Return Convert.ToInt32(cmd.ExecuteScalar()) > 0
            End Using
        End Using
    End Function

    Public Sub CreateUser(username As String, password As String)
        Using con As New SqlConnection(connectionString)
            con.Open()
            Using transaction = con.BeginTransaction()
                Dim userID As Integer
                Using cmd As New SqlCommand("INSERT INTO mn.UUser (Username, PasswordHash) OUTPUT INSERTED.UserID VALUES (@u, @p)", con, transaction)
                    cmd.Parameters.AddWithValue("@u", username.Trim())
                    cmd.Parameters.AddWithValue("@p", HashPassword(password))
                    userID = Convert.ToInt32(cmd.ExecuteScalar())
                End Using

                Using cmd As New SqlCommand("INSERT INTO mn.UserRole (UserID, RoleID) SELECT @userID, RoleID FROM mn.Role WHERE RoleName = 'Admin'", con, transaction)
                    cmd.Parameters.AddWithValue("@userID", userID)
                    cmd.ExecuteNonQuery()
                End Using
                transaction.Commit()
            End Using
        End Using
    End Sub

    Sub CreateInitialUser()
        CreateUser("nati", "admin123")
        MessageBox.Show("Initial user created: nati")
    End Sub

    Private Function HashPassword(password As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            Dim bytes() As Byte = Encoding.UTF8.GetBytes(password)
            Dim hash() As Byte = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function
End Module
