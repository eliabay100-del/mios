Imports System.Data.SqlClient
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Public Class BackupForm
    Inherits Form

    Public Sub New()
        If Not SecurityContext.HasRole("Admin") Then Throw New UnauthorizedAccessException("Only administrators can create backups.")
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Database backup"
        Me.Size = New Size(520, 250)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Controls.Add(New Label With {.Text = "Database backup", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .Location = New Point(28, 24), .AutoSize = True})
        Me.Controls.Add(New Label With {.Text = "Choose a local .bak file location. SQL Server must have write access to the selected folder.", .Location = New Point(30, 78), .Size = New Size(440, 48), .ForeColor = ModernTheme.MutedText})
        Dim btn As Button = ModernTheme.MakePrimaryButton("Create backup")
        btn.Location = New Point(310, 150)
        btn.Width = 150
        AddHandler btn.Click, AddressOf CreateBackup_Click
        Me.Controls.Add(btn)
    End Sub

    Private Sub CreateBackup_Click(sender As Object, e As EventArgs)
        Using dialog As New SaveFileDialog()
            dialog.Filter = "SQL Server backup (*.bak)|*.bak"
            dialog.FileName = "MIOS_Inventory_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".bak"
            If dialog.ShowDialog() <> DialogResult.OK Then Return

            Dim fullPath = dialog.FileName
            If Not ValidateBackupPath(fullPath) Then
                MessageBox.Show("Invalid backup path. Choose a different location.", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Try
                Using conn As New SqlConnection(DBModule.ConnString)
                    conn.Open()
                    Dim safePath = fullPath.Replace("'", "''")
                    Using cmd As New SqlCommand("BACKUP DATABASE [Inventory] TO DISK = N'" & safePath & "' WITH INIT, FORMAT", conn)
                        cmd.CommandTimeout = 0
                        cmd.ExecuteNonQuery()
                    End Using
                End Using
                AppLogger.Info("Database backup completed to " & fullPath)
                MessageBox.Show("Backup completed.", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                AppLogger.Error("Database backup failed", ex)
                MessageBox.Show("Backup failed: " & ex.Message, "Backup", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Function ValidateBackupPath(path As String) As Boolean
        If String.IsNullOrWhiteSpace(path) Then Return False
        If Not path.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) Then Return False
        Dim invalidChars = IO.Path.GetInvalidPathChars()
        If path.IndexOfAny(invalidChars) >= 0 Then Return False
        Dim dir = IO.Path.GetDirectoryName(path)
        If String.IsNullOrEmpty(dir) Then Return False
        If Not IO.Directory.Exists(dir) Then Return False
        Dim fileName = IO.Path.GetFileName(path)
        If String.IsNullOrEmpty(fileName) OrElse fileName.IndexOfAny(IO.Path.GetInvalidFileNameChars()) >= 0 Then Return False
        Return True
    End Function
End Class
