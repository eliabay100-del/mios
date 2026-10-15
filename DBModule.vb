Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Windows.Forms

Module DBModule

    Public ReadOnly Property ConnString As String
        Get
            Dim cs = ConfigurationManager.ConnectionStrings("InventoryDB")
            If cs Is Nothing Then
                Throw New InvalidOperationException("Connection string 'InventoryDB' not found in configuration.")
            End If
            Return cs.ConnectionString
        End Get
    End Property

End Module
