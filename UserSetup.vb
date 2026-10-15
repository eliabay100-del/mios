Imports System.Data.SqlClient
Imports System.Windows.Forms

Module UserSetup

    Public Function HasUsers() As Boolean
        Using con As New SqlConnection(DBModule.ConnString)
            con.Open()
            Using cmd As New SqlCommand("SELECT COUNT(*) FROM mn.UUser", con)
                Return Convert.ToInt32(cmd.ExecuteScalar()) > 0
            End Using
        End Using
    End Function

    Public Sub CreateUser(username As String, password As String)
        Using con As New SqlConnection(DBModule.ConnString)
            con.Open()
            Using transaction = con.BeginTransaction()
                Dim userID As Integer
                Using cmd As New SqlCommand("INSERT INTO mn.UUser (Username, PasswordHash) OUTPUT INSERTED.UserID VALUES (@u, @p)", con, transaction)
                    cmd.Parameters.AddWithValue("@u", username.Trim())
                    cmd.Parameters.AddWithValue("@p", SecurityService.HashPassword(password))
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

End Module
