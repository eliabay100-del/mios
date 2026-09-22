Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Public Module ModernTheme
    Public ReadOnly PageBackground As Color = Color.FromArgb(245, 243, 239)
    Public ReadOnly Surface As Color = Color.FromArgb(255, 255, 255)
    Public ReadOnly SurfaceAlt As Color = Color.FromArgb(246, 247, 244)
    Public ReadOnly PrimaryText As Color = Color.FromArgb(24, 29, 35)
    Public ReadOnly MutedText As Color = Color.FromArgb(96, 106, 119)
    Public ReadOnly Border As Color = Color.FromArgb(224, 227, 231)
    Public ReadOnly Accent As Color = Color.FromArgb(48, 145, 86)
    Public ReadOnly AccentDark As Color = Color.FromArgb(34, 108, 64)
    Public ReadOnly AccentSoft As Color = Color.FromArgb(225, 241, 229)
    Public ReadOnly Success As Color = Color.FromArgb(46, 146, 105)
    Public ReadOnly Danger As Color = Color.FromArgb(203, 78, 78)
    Public ReadOnly Warning As Color = Color.FromArgb(212, 150, 78)

    Public Sub ApplyWindowTheme(form As Form)
        form.BackColor = PageBackground
        form.ForeColor = PrimaryText
        form.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)

        ' MIOS builds much of its UI in code rather than through the WinForms designer.
        ' Keep WinForms font scaling from fighting the explicit DPI layout scaling below.
        form.AutoScaleMode = AutoScaleMode.None
        form.AutoScroll = True
        form.AutoScrollMinSize = New Size(0, form.ClientSize.Height + 1)

        AddHandler form.Shown, Sub(sender, e)
                                    StyleButtons(form)
                                    ScaleForCurrentDpi(form)
                                End Sub
        AddHandler LanguageService.LanguageChanged, Sub(sender, e) LanguageService.ApplyTo(form)
    End Sub

    ''' <summary>
    ''' Scales the complete runtime-created layout from the 96-DPI design baseline.
    ''' This is important because MIOS creates many controls after InitializeComponent().
    ''' </summary>
    Public Sub ScaleForCurrentDpi(form As Form)
        Dim dpi As Single = 96.0F

        Try
            Using graphics As Graphics = form.CreateGraphics()
                dpi = graphics.DpiX
            End Using
        Catch
            Return
        End Try

        Dim scale As Single = dpi / 96.0F
        If Math.Abs(scale - 1.0F) < 0.01F Then Return

        ' Scale fonts as well as geometry so explicit runtime-created fonts
        ' remain proportional at 125%, 150%, 175%, etc. Inherited fonts are
        ' tracked so they are not accidentally scaled more than once.
        ScaleControlTree(form, scale, form.Font)
        form.Font = ScaleFont(form.Font, scale)

        If form.WindowState <> FormWindowState.Maximized Then
            form.Size = ScaleSize(form.Size, scale)
        End If

        If form.MinimumSize.Width > 0 OrElse form.MinimumSize.Height > 0 Then
            form.MinimumSize = ScaleSize(form.MinimumSize, scale)
        End If

        If form.MaximumSize.Width > 0 OrElse form.MaximumSize.Height > 0 Then
            form.MaximumSize = ScaleSize(form.MaximumSize, scale)
        End If

        If form.AutoScroll Then
            form.AutoScrollMinSize = ScaleSize(form.AutoScrollMinSize, scale)
        End If
    End Sub

    Private Sub ScaleControlTree(parent As Control, scale As Single, inheritedFont As Font)
        For Each child As Control In parent.Controls
            child.Location = ScalePoint(child.Location, scale)

            If Not child.AutoSize Then
                child.Size = ScaleSize(child.Size, scale)
            End If

            child.Padding = ScalePadding(child.Padding, scale)
            child.Margin = ScalePadding(child.Margin, scale)

            Dim originalFont As Font = child.Font
            If originalFont.Equals(inheritedFont) Then
                child.Font = ScaleFont(inheritedFont, scale)
            Else
                child.Font = ScaleFont(originalFont, scale)
            End If

            If child.MinimumSize.Width > 0 OrElse child.MinimumSize.Height > 0 Then
                child.MinimumSize = ScaleSize(child.MinimumSize, scale)
            End If

            If child.MaximumSize.Width > 0 OrElse child.MaximumSize.Height > 0 Then
                child.MaximumSize = ScaleSize(child.MaximumSize, scale)
            End If

            Dim table = TryCast(child, TableLayoutPanel)
            If table IsNot Nothing Then
                For Each style As ColumnStyle In table.ColumnStyles
                    If style.SizeType = SizeType.Absolute Then
                        style.Width *= scale
                    End If
                Next

                For Each style As RowStyle In table.RowStyles
                    If style.SizeType = SizeType.Absolute Then
                        style.Height *= scale
                    End If
                Next
            End If

            ScaleControlTree(child, scale, originalFont)
        Next
    End Sub

    Private Function ScalePoint(value As Point, scale As Single) As Point
        Return New Point(
            CInt(Math.Round(value.X * scale)),
            CInt(Math.Round(value.Y * scale))
        )
    End Function

    Private Function ScaleSize(value As Size, scale As Single) As Size
        Return New Size(
            Math.Max(0, CInt(Math.Round(value.Width * scale))),
            Math.Max(0, CInt(Math.Round(value.Height * scale)))
        )
    End Function

    Private Function ScaleFont(value As Font, scale As Single) As Font
        If value Is Nothing Then Return value
        Return New Font(value.FontFamily, value.Size * scale, value.Style, value.Unit, value.GdiCharSet, value.GdiVerticalFont)
    End Function

    Private Function ScalePadding(value As Padding, scale As Single) As Padding
        Return New Padding(
            Math.Max(0, CInt(Math.Round(value.Left * scale))),
            Math.Max(0, CInt(Math.Round(value.Top * scale))),
            Math.Max(0, CInt(Math.Round(value.Right * scale))),
            Math.Max(0, CInt(Math.Round(value.Bottom * scale)))
        )
    End Function

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
        btn.BackColor = Accent
        btn.ForeColor = Color.White
        btn.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        btn.Cursor = Cursors.Hand
        btn.UseVisualStyleBackColor = False
        btn.Padding = New Padding(14, 8, 14, 8)
        btn.Height = 40
        Return btn
    End Function

    Private Sub StyleButtons(root As Control)
        If TypeOf root Is Button Then
            Dim button = DirectCast(root, Button)
            button.BackColor = Accent
            button.ForeColor = Color.White
            button.FlatStyle = FlatStyle.Flat
            button.FlatAppearance.BorderSize = 0
            button.UseVisualStyleBackColor = False
            button.Cursor = Cursors.Hand
            button.FlatAppearance.MouseDownBackColor = AccentDark
            button.FlatAppearance.MouseOverBackColor = AccentDark
        End If
        For Each child As Control In root.Controls
            StyleButtons(child)
        Next
    End Sub

    Private Sub RoundButton(button As Button)
        If button.Width <= 0 OrElse button.Height <= 0 Then Return
        Using path As New GraphicsPath()
            Dim radius As Integer = Math.Min(10, Math.Min(button.Width, button.Height) \ 3)
            Dim diameter As Integer = radius * 2
            path.AddArc(0, 0, diameter, diameter, 180, 90)
            path.AddArc(button.Width - diameter, 0, diameter, diameter, 270, 90)
            path.AddArc(button.Width - diameter, button.Height - diameter, diameter, diameter, 0, 90)
            path.AddArc(0, button.Height - diameter, diameter, diameter, 90, 90)
            path.CloseFigure()
            button.Region = New Region(path)
        End Using
    End Sub

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
