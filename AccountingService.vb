Imports System.Data.SqlClient

Public Class JournalLineData
    Public Property AccountCode As String
    Public Property Debit As Decimal
    Public Property Credit As Decimal
End Class

Public Module AccountingService
    Public Sub WriteAudit(conn As SqlConnection, transaction As SqlTransaction, actionCode As String, entityType As String, entityID As Integer, details As String)
        Const auditSql As String = "INSERT INTO mn.AuditLog (UserID, ActionCode, EntityType, EntityID, Details) VALUES (@userID, @actionCode, @entityType, @entityID, @details)"
        Using cmd As New SqlCommand(auditSql, conn, transaction)
            cmd.Parameters.AddWithValue("@userID", If(SecurityContext.CurrentUserID = 0, DBNull.Value, CObj(SecurityContext.CurrentUserID)))
            cmd.Parameters.AddWithValue("@actionCode", actionCode)
            cmd.Parameters.AddWithValue("@entityType", entityType)
            cmd.Parameters.AddWithValue("@entityID", entityID)
            cmd.Parameters.AddWithValue("@details", If(String.IsNullOrEmpty(details), DBNull.Value, CObj(details)))
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Public Sub PostJournalEntry(conn As SqlConnection, transaction As SqlTransaction, referenceType As String, referenceID As Integer, description As String, lines As IEnumerable(Of JournalLineData))
        Dim journalLines As List(Of JournalLineData) = lines.ToList()
        Dim debitTotal As Decimal = journalLines.Sum(Function(line) line.Debit)
        Dim creditTotal As Decimal = journalLines.Sum(Function(line) line.Credit)

        If debitTotal <> creditTotal Then
            Throw New InvalidOperationException("Accounting entry is not balanced.")
        End If

        Dim entryID As Integer
        Const entrySql As String = "INSERT INTO mn.JournalEntry (ReferenceType, ReferenceID, Description) OUTPUT INSERTED.JournalEntryID VALUES (@type, @referenceID, @description)"
        Using cmd As New SqlCommand(entrySql, conn, transaction)
            cmd.Parameters.AddWithValue("@type", referenceType)
            cmd.Parameters.AddWithValue("@referenceID", referenceID)
            cmd.Parameters.AddWithValue("@description", description)
            entryID = Convert.ToInt32(cmd.ExecuteScalar())
        End Using

        Const accountSql As String = "SELECT AccountID FROM mn.Account WHERE AccountCode = @code AND IsActive = 1"
        Const lineSql As String = "INSERT INTO mn.JournalLine (JournalEntryID, AccountID, Debit, Credit) VALUES (@entryID, @accountID, @debit, @credit)"
        For Each line As JournalLineData In journalLines
            Dim accountID As Integer
            Using cmd As New SqlCommand(accountSql, conn, transaction)
                cmd.Parameters.AddWithValue("@code", line.AccountCode)
                Dim result As Object = cmd.ExecuteScalar()
                If result Is Nothing Then Throw New InvalidOperationException("Account is not configured: " & line.AccountCode)
                accountID = Convert.ToInt32(result)
            End Using

            Using cmd As New SqlCommand(lineSql, conn, transaction)
                cmd.Parameters.AddWithValue("@entryID", entryID)
                cmd.Parameters.AddWithValue("@accountID", accountID)
                cmd.Parameters.AddWithValue("@debit", line.Debit)
                cmd.Parameters.AddWithValue("@credit", line.Credit)
                cmd.ExecuteNonQuery()
            End Using
        Next

        WriteAudit(conn, transaction, "JOURNAL_POSTED", "JournalEntry", entryID, description)
    End Sub
End Module
