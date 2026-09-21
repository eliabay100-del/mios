Imports System.Drawing
Imports System.Windows.Forms

Public Module ModernTheme
    Public ReadOnly PageBackground As Color = Color.FromArgb(14, 16, 15)
    Public ReadOnly Surface As Color = Color.FromArgb(23, 27, 25)
    Public ReadOnly SurfaceAlt As Color = Color.FromArgb(31, 36, 33)
    Public ReadOnly PrimaryText As Color = Color.FromArgb(244, 241, 232)
    Public ReadOnly MutedText As Color = Color.FromArgb(157, 164, 156)
    Public ReadOnly Border As Color = Color.FromArgb(54, 61, 56)
    Public ReadOnly Accent As Color = Color.FromArgb(170, 196, 126)
    Public ReadOnly AccentDark As Color = Color.FromArgb(108, 137, 76)
    Public ReadOnly AccentSoft As Color = Color.FromArgb(49, 63, 43)
    Public ReadOnly Success As Color = Color.FromArgb(139, 190, 116)
    Public ReadOnly Danger As Color = Color.FromArgb(222, 112, 101)
    Public ReadOnly Warning As Color = Color.FromArgb(222, 173, 93)

    Public Sub ApplyWindowTheme(form As Form)
        form.BackColor = PageBackground
        form.ForeColor = PrimaryText
        form.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
    End Sub

    Public Function MakePrimaryButton(text As String) As Button
        Dim btn As New Button
        btn.Text = text
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.BackColor = Accent
        btn.ForeColor = Color.FromArgb(17, 21, 18)
        btn.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        btn.Cursor = Cursors.Hand
        btn.UseVisualStyleBackColor = False
        btn.Padding = New Padding(14, 8, 14, 8)
        btn.Height = 40
        Return btn
    End Function

    Public Function MakeSecondaryButton(text As String) As Button
        Dim btn As New Button
        btn.Text = text
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.BackColor = SurfaceAlt
        btn.ForeColor = PrimaryText
        btn.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        btn.Cursor = Cursors.Hand
        btn.UseVisualStyleBackColor = False
        btn.Padding = New Padding(14, 8, 14, 8)
        btn.Height = 40
        Return btn
    End Function

    Public Function MakeTextBox() As TextBox
        Dim txt As New TextBox
        txt.BackColor = SurfaceAlt
        txt.ForeColor = PrimaryText
        txt.BorderStyle = BorderStyle.FixedSingle
        txt.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
        txt.Height = 42
        Return txt
    End Function

    Public Sub StyleGrid(grid As DataGridView)
        grid.BackgroundColor = Surface
        grid.BorderStyle = BorderStyle.None
        grid.EnableHeadersVisualStyles = False
        grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt
        grid.ColumnHeadersDefaultCellStyle.ForeColor = PrimaryText
        grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        grid.DefaultCellStyle.BackColor = Surface
        grid.DefaultCellStyle.ForeColor = PrimaryText
        grid.DefaultCellStyle.SelectionBackColor = AccentSoft
        grid.DefaultCellStyle.SelectionForeColor = PrimaryText
        grid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt
        grid.GridColor = Border
        grid.RowHeadersVisible = False
        grid.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
    End Sub
End Module
