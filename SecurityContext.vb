Imports System.Data.SqlClient

''' <summary>
''' The permissions of the signed-in user. Permissions are loaded once per
''' session instead of querying the database on every check.
''' </summary>
Public Module SecurityContext
    Public Property CurrentUserID As Integer
    Public Property CurrentUsername As String = ""
    Public Property CurrentRole As String = ""

    Private ReadOnly permissions As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    Private permissionsLoaded As Boolean = False

    ''' <summary>Loads (and caches) every permission granted to the current user.</summary>
    Public Sub LoadPermissions()
        permissions.Clear()
        permissionsLoaded = False
        If CurrentUserID = 0 Then Return

        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Dim column As String = PermissionColumnName(conn)
                Dim query As String = "SELECT p." & column & " FROM mn.UserRole ur " &
                                      "JOIN mn.RolePermission rp ON rp.RoleID = ur.RoleID " &
                                      "JOIN mn.Permission p ON p.PermissionID = rp.PermissionID " &
                                      "WHERE ur.UserID = @userID"
                Using cmd As New SqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@userID", CurrentUserID)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            If Not reader.IsDBNull(0) Then permissions.Add(reader.GetString(0))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load permissions for user " & CurrentUserID, ex)
        Finally
            ' Load once per session. If the load failed we still mark it done,
            ' otherwise every HasPermission call would hammer the database with a
            ' fresh connection and a fresh log entry (fails closed either way).
            permissionsLoaded = True
        End Try
    End Sub

    ''' <summary>
    ''' The permission tables have shipped under two schemas: older databases use
    ''' mn.Permission.PermissionName, newer ones use PermissionCode. Detect which
    ''' column this database actually has instead of assuming one.
    ''' </summary>
    Private Function PermissionColumnName(conn As SqlConnection) As String
        Const sql As String = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS " &
                              "WHERE TABLE_SCHEMA = 'mn' AND TABLE_NAME = 'Permission' " &
                              "AND COLUMN_NAME IN ('PermissionName', 'PermissionCode')"
        Using cmd As New SqlCommand(sql, conn)
            Dim value As Object = cmd.ExecuteScalar()
            If value IsNot Nothing AndAlso value IsNot DBNull.Value Then Return Convert.ToString(value)
        End Using
        Return "PermissionName"
    End Function

    Public Function HasRole(ParamArray roles As String()) As Boolean
        Return roles.Any(Function(role) String.Equals(role, CurrentRole, StringComparison.OrdinalIgnoreCase))
    End Function

    Public Function HasPermission(permissionCode As String) As Boolean
        If CurrentUserID = 0 Then Return False
        If Not permissionsLoaded Then LoadPermissions()
        Return permissions.Contains(permissionCode)
    End Function

    Public Sub Clear()
        CurrentUserID = 0
        CurrentUsername = ""
        CurrentRole = ""
        permissions.Clear()
        permissionsLoaded = False
    End Sub
End Module
