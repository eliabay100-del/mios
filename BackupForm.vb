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
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.WindowState = FormWindowState.Maximized
        Me.AutoScroll = True
        Me.Controls.Add(New Label With {.Text = "Database backup", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .Location = New Point(28, 24), .AutoSize = True})
        Me.Controls.Add(New Label With {.Text = "Choose a local .bak file location. SQL Server must have write access to the selected folder.", .Location = New Point(30, 78), .Size = New Size(440, 48), .ForeColor = ModernTheme.MutedText})
        Dim btn As Button = ModernTheme.MakePrimaryButton("Create backup")
        btn.Location = New Point(310, 150)
        btn.Width = 150
        AddHandler btn.Click, AddressOf CreateBackup_Click
        Me.Controls.Add(btn)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Close")
        btnClose.Location = New Point(170, 150)
        btnClose.Width = 130
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)

        Me.AcceptButton = btn
        Me.CancelButton = btnClose
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

            Dim btn As Button = TryCast(sender, Button)
            Try
                ' A full database backup can take many seconds; show a busy cursor
                ' and disable the button so the window does not look hung.
                Me.Cursor = Cursors.WaitCursor
                If btn IsNot Nothing Then btn.Enabled = False
                Using conn As New SqlConnection(DBModule.ConnString)
                    conn.Open()
                    ' Quote the database name from the server (never user input) and
                    ' pass the destination as a parameter instead of concatenating it.
                    Dim dbName As String = "[" & conn.Database.Replace("]", "]]") & "]"
                    Using cmd As New SqlCommand("BACKUP DATABASE " & dbName & " TO DISK = @path WITH INIT, FORMAT", conn)
                        cmd.Parameters.AddWithValue("@path", fullPath)
                        cmd.CommandTimeout = 0
                        cmd.ExecuteNonQuery()
                    End Using
                End Using
                Me.Cursor = Cursors.Default
                If btn IsNot Nothing Then btn.Enabled = True
                AppLogger.Info("Database backup completed to " & fullPath)
                MessageBox.Show("Backup completed.", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                Me.Cursor = Cursors.Default
                If btn IsNot Nothing Then btn.Enabled = True
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
