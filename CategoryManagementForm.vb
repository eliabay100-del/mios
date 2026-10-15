Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class CategoryManagementForm
    Inherits Form

    Private ReadOnly txtCategory As New TextBox()
    Private ReadOnly lstCategories As New ListBox()

    Public Sub New()
        If Not SecurityContext.HasRole("Admin") Then Throw New UnauthorizedAccessException("Only administrators can manage categories.")
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Manage categories"
        Me.Size = New Size(520, 480)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        Me.Controls.Add(New Label With {
            .Text = "Manage categories",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .Location = New Point(28, 24),
            .AutoSize = True
        })

        Me.Controls.Add(New Label With {
            .Text = "Add a category for spare parts",
            .ForeColor = ModernTheme.MutedText,
            .Location = New Point(30, 70),
            .AutoSize = True
        })

        txtCategory.Location = New Point(30, 105)
        txtCategory.Width = 320
        txtCategory.Height = 30
        Me.Controls.Add(txtCategory)

        Dim btnAdd As Button = ModernTheme.MakePrimaryButton("Add category")
        btnAdd.Location = New Point(360, 101)
        btnAdd.Width = 125
        AddHandler btnAdd.Click, AddressOf AddCategory_Click
        Me.Controls.Add(btnAdd)

        lstCategories.Location = New Point(30, 160)
        lstCategories.Size = New Size(455, 230)
        lstCategories.Font = New Font("Segoe UI", 11)
        lstCategories.BackColor = ModernTheme.Surface
        lstCategories.ForeColor = ModernTheme.PrimaryText
        Me.Controls.Add(lstCategories)

        Dim btnClose As Button = ModernTheme.MakeSecondaryButton("Close")
        btnClose.Location = New Point(360, 405)
        btnClose.Width = 125
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)

        LoadCategories()
    End Sub

    Private Sub LoadCategories()
        lstCategories.Items.Clear()
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using cmd As New SqlCommand("SELECT CategoryName FROM mn.Category ORDER BY CategoryName", conn)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            lstCategories.Items.Add(reader.GetString(0))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load categories", ex)
            MessageBox.Show("Unable to load categories: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AddCategory_Click(sender As Object, e As EventArgs)
        Dim categoryName As String = txtCategory.Text.Trim()
        If String.IsNullOrWhiteSpace(categoryName) Then
            MessageBox.Show("Enter a category name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCategory.Focus()
            Return
        End If
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using cmd As New SqlCommand("INSERT INTO mn.Category (CategoryName) SELECT @name WHERE NOT EXISTS (SELECT 1 FROM mn.Category WHERE LOWER(CategoryName) = LOWER(@name))", conn)
                    cmd.Parameters.AddWithValue("@name", categoryName)
                    If cmd.ExecuteNonQuery() = 0 Then
                        MessageBox.Show("That category already exists.", "Duplicate category", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return
                    End If
                End Using
            End Using
            txtCategory.Clear()
            LoadCategories()
        Catch ex As Exception
            AppLogger.Error("Failed to add category", ex)
            MessageBox.Show("Unable to add category: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
