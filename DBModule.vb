Imports System.Data
Imports System.Data.SqlClient
Imports System.Windows.Forms

Module DBModule

    Public ReadOnly ConnString As String =
        "Server=DESKTOP-KGESGJQ\NATNAEL;Database=INVENTORY;Trusted_Connection=True;"

    Public Conn As SqlConnection

    Public Sub OpenConnection()
        If Conn Is Nothing Then
            Conn = New SqlConnection(ConnString)
        End If

        If Conn.State = ConnectionState.Closed Then
            Conn.Open()
        End If
    End Sub

    Public Sub CloseConnection()
        If Conn IsNot Nothing AndAlso Conn.State = ConnectionState.Open Then
            Conn.Close()
        End If
    End Sub

End Module

