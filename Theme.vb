Imports System.Drawing
Imports System.Windows.Forms

Public Module ModernTheme
    Public ReadOnly PageBackground As Color = Color.FromArgb(245, 243, 239)
    Public ReadOnly Surface As Color = Color.FromArgb(255, 255, 255)
    Public ReadOnly SurfaceAlt As Color = Color.FromArgb(246, 247, 244)
    Public ReadOnly PrimaryText As Color = Color.FromArgb(24, 29, 35)
    Public ReadOnly MutedText As Color = Color.FromArgb(96, 106, 119)
    Public ReadOnly Border As Color = Color.FromArgb(224, 227, 231)
    Public ReadOnly Accent As Color = Color.FromArgb(31, 91, 143)
    Public ReadOnly AccentDark As Color = Color.FromArgb(18, 56, 87)
    Public ReadOnly AccentSoft As Color = Color.FromArgb(232, 238, 247)
    Public ReadOnly Success As Color = Color.FromArgb(46, 146, 105)
    Public ReadOnly Danger As Color = Color.FromArgb(203, 78, 78)
    Public ReadOnly Warning As Color = Color.FromArgb(212, 150, 78)

    Public Sub ApplyWindowTheme(form As Form)
        form.BackColor = PageBackground
        form.ForeColor = PrimaryText
        form.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
        form.AutoScroll = True
        form.AutoScrollMinSize = New Size(0, form.ClientSize.Height + 1)
    End Sub

    Public Sub EnableVerticalScroll(form As Form, contentHeight As Integer)
        form.AutoScroll = True
        form.AutoScrollMinSize = New Size(0, Math.Max(contentHeight, form.ClientSize.Height + 1))
        form.VerticalScroll.Visible = True
    End Sub

    Public Function MakePrimaryButton(text As String) As Button
        Dim btn As New Button
        btn.Text = text
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.BackColor = Accent
        btn.ForeColor = Color.White
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
        btn.BackColor = Surface
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
        txt.BackColor = Color.White
        txt.ForeColor = PrimaryText
        txt.BorderStyle = BorderStyle.FixedSingle
        txt.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
        txt.Height = 42
        Return txt
    End Function

    Public Sub StyleGrid(grid As DataGridView)
        grid.BackgroundColor = Color.White
        grid.BorderStyle = BorderStyle.None
        grid.EnableHeadersVisualStyles = False
        grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt
        grid.ColumnHeadersDefaultCellStyle.ForeColor = PrimaryText
        grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        grid.DefaultCellStyle.BackColor = Color.White
        grid.DefaultCellStyle.ForeColor = PrimaryText
        grid.DefaultCellStyle.SelectionBackColor = AccentSoft
        grid.DefaultCellStyle.SelectionForeColor = PrimaryText
        grid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt
        grid.GridColor = Border
        grid.RowHeadersVisible = False
        grid.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
    End Sub
End Module
