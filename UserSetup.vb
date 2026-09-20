Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Text
Imports System.Windows.Forms

Module UserSetup
    Private connectionString As String = "Server=DESKTOP-KGESGJQ\NATNAEL;Database=Inventory;Integrated Security=True;"

    Sub CreateInitialUser()
        Dim username As String = "nati"
        Dim password As String = "admin123"
        Dim hash As String = HashPassword(password)

        Using con As New SqlConnection(connectionString)
            con.Open()
            Dim cmd As New SqlCommand("INSERT INTO mn.UUser (Username, PasswordHash) VALUES (@u, @p)", con)
            cmd.Parameters.AddWithValue("@u", username)
            cmd.Parameters.AddWithValue("@p", hash)
            cmd.ExecuteNonQuery()
        End Using

        MessageBox.Show("Initial user created: " & username)
    End Sub

    Private Function HashPassword(password As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            Dim bytes() As Byte = Encoding.UTF8.GetBytes(password)
            Dim hash() As Byte = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function
End Module
