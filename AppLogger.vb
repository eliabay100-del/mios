Imports System.IO

Module AppLogger

    Private ReadOnly LogDir As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MIOS", "Logs")

    Public Sub Info(message As String)
        WriteLog("INFO", message, Nothing)
    End Sub

    Public Sub [Error](message As String, ex As Exception)
        WriteLog("ERROR", message, ex)
    End Sub

    Public Sub Warn(message As String)
        WriteLog("WARN", message, Nothing)
    End Sub

    Private Sub WriteLog(level As String, message As String, ex As Exception)
        Try
            If Not Directory.Exists(LogDir) Then Directory.CreateDirectory(LogDir)
            Dim logFile = Path.Combine(LogDir, DateTime.Now.ToString("yyyy-MM-dd") & ".log")
            Dim timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            Dim line = timestamp & " [" & level & "] " & message
            If ex IsNot Nothing Then line &= vbCrLf & ex.ToString()
            File.AppendAllText(logFile, line & vbCrLf)
        Catch
        End Try
    End Sub

End Module
