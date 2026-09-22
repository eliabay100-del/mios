Public Module SecurityContext
    Public Property CurrentUserID As Integer
    Public Property CurrentUsername As String = ""
    Public Property CurrentRole As String = ""

    Public Function HasRole(ParamArray roles As String()) As Boolean
        Return roles.Any(Function(role) String.Equals(role, CurrentRole, StringComparison.OrdinalIgnoreCase))
    End Function
    Public Function HasPermission(permissionCode As String) As Boolean
        If CurrentUserID = 0 Then Return False
        Try
            Using conn As New SqlClient.SqlConnection(DBModule.ConnString)
                conn.Open()
                Const query As String = "SELECT COUNT(*) FROM mn.UserRole ur JOIN mn.RolePermission rp ON rp.RoleID = ur.RoleID JOIN mn.Permission p ON p.PermissionID = rp.PermissionID WHERE ur.UserID = @userID AND p.PermissionCode = @permission"
                Using cmd As New SqlClient.SqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@userID", CurrentUserID)
                    cmd.Parameters.AddWithValue("@permission", permissionCode)
                    Return Convert.ToInt32(cmd.ExecuteScalar()) > 0
                End Using
            End Using
        Catch
            Return False
        End Try
    End Function
End Module
