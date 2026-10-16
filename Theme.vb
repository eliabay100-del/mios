Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.CompilerServices
Imports System.Windows.Forms

''' <summary>
''' Shared visual styling for MIOS. MIOS builds most of its UI in code at a
''' 96-DPI baseline, so this module also handles DPI scaling for that layout.
''' </summary>
Public Module ModernTheme

    ' ---- Palette --------------------------------------------------------
    Public ReadOnly PageBackground As Color = Color.FromArgb(243, 245, 247)
    Public ReadOnly Surface As Color = Color.FromArgb(255, 255, 255)
    Public ReadOnly SurfaceAlt As Color = Color.FromArgb(247, 249, 250)
    Public ReadOnly PrimaryText As Color = Color.FromArgb(23, 28, 35)
    Public ReadOnly MutedText As Color = Color.FromArgb(100, 112, 126)
    Public ReadOnly Border As Color = Color.FromArgb(222, 226, 231)
    Public ReadOnly Accent As Color = Color.FromArgb(37, 138, 82)
    Public ReadOnly AccentDark As Color = Color.FromArgb(28, 110, 65)
    Public ReadOnly AccentSoft As Color = Color.FromArgb(226, 242, 232)
    Public ReadOnly Success As Color = Color.FromArgb(46, 146, 105)
    Public ReadOnly Danger As Color = Color.FromArgb(198, 71, 71)
    Public ReadOnly Warning As Color = Color.FromArgb(200, 145, 70)

    ''' <summary>Comfortable default height that keeps button text fully visible.</summary>
    Public Const ControlHeight As Integer = 44

    ' Tracks controls that have already been DPI-scaled so repeated layout
    ' rebuilds never scale the same control twice. Weakly referenced so the
    ' entries do not keep disposed controls alive.
    Private ReadOnly scaledControls As New ConditionalWeakTable(Of Control, Object)

    ' ---- Window theming -------------------------------------------------

    Public Sub ApplyWindowTheme(form As Form)
        form.BackColor = PageBackground
        form.ForeColor = PrimaryText
        If form.Font Is Nothing OrElse form.Font.FontFamily.Name <> "Segoe UI" Then
            form.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
        End If

        Dim appIcon As Icon = LoadIcon()
        If appIcon IsNot Nothing Then form.Icon = appIcon

        ' MIOS lays out controls at a 96-DPI baseline and scales them itself,
        ' so WinForms' own font auto-scaling stays off.
        form.AutoScaleMode = AutoScaleMode.None
        form.AutoScroll = True

        AddHandler form.Shown, Sub(sender, e)
                                    ScaleForCurrentDpi(form)
                                    StyleContent(form)
                                End Sub

        ' The language subscription lives on a module-level event, so it would pin
        ' every form that was ever opened for the lifetime of the process. Remove
        ' it as soon as the form closes.
        Dim languageHandler As EventHandler = Sub(sender, e) LanguageService.ApplyTo(form)
        AddHandler LanguageService.LanguageChanged, languageHandler
        AddHandler form.FormClosed, Sub(sender, e) RemoveHandler LanguageService.LanguageChanged, languageHandler
    End Sub

    Public Sub EnableVerticalScroll(form As Form, contentHeight As Integer)
        form.AutoScroll = True
        form.AutoScrollMinSize = New Size(0, Math.Max(contentHeight, form.ClientSize.Height + 1))
    End Sub

    ''' <summary>
    ''' Styles and DPI-scales content that was added to a form after it was shown
    ''' (for example, the dynamically rebuilt sections of the issue/receive forms).
    ''' </summary>
    Public Sub RefreshContent(root As Control)
        ScaleForCurrentDpi(root)
        StyleContent(root)
    End Sub

    ' ---- Buttons --------------------------------------------------------

    Public Function MakePrimaryButton(text As String) As Button
        Return CreateButton(text, Accent, Color.White, includeBorder:=False)
    End Function

    Public Function MakeSecondaryButton(text As String) As Button
        Return CreateButton(text, Surface, PrimaryText, includeBorder:=True)
    End Function

    Public Function MakeDangerButton(text As String) As Button
        Return CreateButton(text, Danger, Color.White, includeBorder:=False)
    End Function

    Private Function CreateButton(text As String, back As Color, fore As Color, includeBorder As Boolean) As Button
        Dim btn As New Button With {
            .Text = text,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = back,
            .ForeColor = fore,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .Cursor = Cursors.Hand,
            .UseVisualStyleBackColor = False,
            .UseCompatibleTextRendering = False,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Height = ControlHeight,
            .UseMnemonic = False
        }
        btn.FlatAppearance.BorderSize = If(includeBorder, 1, 0)
        btn.FlatAppearance.BorderColor = If(includeBorder, Border, back)
        ApplyHoverColors(btn)
        Return btn
    End Function

    ' ---- Content styling ------------------------------------------------

    Private Sub StyleContent(root As Control)
        If TypeOf root Is Button Then
            StyleButton(DirectCast(root, Button))
        ElseIf TypeOf root Is Label Then
            StyleLabel(DirectCast(root, Label))
        ElseIf TypeOf root Is TextBox Then
            StyleTextBox(DirectCast(root, TextBox))
        ElseIf TypeOf root Is ComboBox Then
            StyleComboBox(DirectCast(root, ComboBox))
        End If

        For Each child As Control In root.Controls
            StyleContent(child)
        Next
    End Sub

    ''' <summary>
    ''' Brings a plain, hand-built TextBox in line with the themed inputs so it
    ''' does not render in the old Microsoft Sans Serif default.
    ''' </summary>
    Private Sub StyleTextBox(textBox As TextBox)
        textBox.BackColor = Color.White
        textBox.ForeColor = PrimaryText
        If Not textBox.Multiline Then textBox.BorderStyle = BorderStyle.FixedSingle
        If textBox.Font Is Nothing OrElse textBox.Font.FontFamily.Name = "Microsoft Sans Serif" Then
            textBox.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
        End If
    End Sub

    ''' <summary>
    ''' Applies the shared input font/colours to a plain, hand-built ComboBox.
    ''' </summary>
    Private Sub StyleComboBox(combo As ComboBox)
        combo.BackColor = Color.White
        combo.ForeColor = PrimaryText
        combo.FlatStyle = FlatStyle.Flat
        If combo.Font Is Nothing OrElse combo.Font.FontFamily.Name = "Microsoft Sans Serif" Then
            combo.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
        End If
    End Sub

    Private Sub StyleButton(button As Button)
        ' Crisp, ClearType-style text rendering instead of the legacy GDI+ path.
        button.UseCompatibleTextRendering = False
        ' Never treat '&' as an accelerator: the text is user-facing copy that may
        ' legitimately contain one (for example "Save customer & issue").
        button.UseMnemonic = False
        button.Cursor = Cursors.Hand
        button.FlatStyle = FlatStyle.Flat
        button.TextAlign = ContentAlignment.MiddleCenter

        If button.Font IsNot Nothing AndAlso button.Font.FontFamily.Name = "Microsoft Sans Serif" Then
            button.Font = New Font("Segoe UI", 10.0F, button.Font.Style)
        End If

        ' Guarantee the label is tall enough for its glyphs so descenders and
        ' bold text are never clipped at the bottom of the button.
        EnforceReadableHeight(button)

        If Not IsTransparent(button.BackColor) Then ApplyHoverColors(button)
    End Sub

    Private Sub StyleLabel(label As Label)
        label.UseCompatibleTextRendering = False
        ' Render '&' literally rather than as a mnemonic prefix.
        label.UseMnemonic = False

        ' Fixed-height labels (titles are a common offender) are often too short
        ' for their font, which cuts the bottom off the text. Give them room.
        If label.AutoSize OrElse label.Dock <> DockStyle.None Then Return
        Dim font As Font = label.Font
        If font Is Nothing Then Return

        Dim sample As String = If(String.IsNullOrEmpty(label.Text), "Ag", label.Text)
        Dim textHeight As Integer = TextRenderer.MeasureText(sample, font, New Size(Integer.MaxValue, Integer.MaxValue), TextFormatFlags.SingleLine Or TextFormatFlags.NoPrefix).Height
        Dim needed As Integer = textHeight + 6
        ' Titles are frequently given a fixed height that is too small for their
        ' font (before and especially after DPI scaling). Grow them to fit; the
        ' cap only guards against pathological overlaps.
        If needed > label.Height AndAlso (needed - label.Height) <= 64 Then
            label.Height = needed
        End If
    End Sub

    Private Sub EnforceReadableHeight(button As Button)
        If button.AutoSize OrElse button.Dock <> DockStyle.None Then Return
        Dim font As Font = button.Font
        If font Is Nothing Then Return

        Dim sample As String = If(String.IsNullOrEmpty(button.Text), "Ag", button.Text)
        Dim textHeight As Integer = TextRenderer.MeasureText(sample, font, New Size(Integer.MaxValue, Integer.MaxValue), TextFormatFlags.SingleLine Or TextFormatFlags.NoPrefix).Height
        Dim needed As Integer = textHeight + button.Padding.Vertical + 10
        If needed > button.Height AndAlso (needed - button.Height) <= 16 Then
            button.Height = needed
        End If
    End Sub

    Private Sub ApplyHoverColors(button As Button)
        Dim back As Color = button.BackColor
        If IsTransparent(back) Then
            button.FlatAppearance.MouseOverBackColor = SurfaceAlt
            button.FlatAppearance.MouseDownBackColor = Border
        Else
            button.FlatAppearance.MouseOverBackColor = Shade(back, -12)
            button.FlatAppearance.MouseDownBackColor = Shade(back, -26)
        End If
    End Sub

    ' ---- DPI scaling ----------------------------------------------------

    ''' <summary>
    ''' Scales a form's code-built layout from the 96-DPI design baseline to the
    ''' current display DPI. Safe to call repeatedly: each control is scaled once.
    ''' </summary>
    Public Sub ScaleForCurrentDpi(control As Control)
        Dim form As Form = TryCast(control, Form)
        If form Is Nothing Then form = control.FindForm()
        If form Is Nothing Then Return

        Dim scale As Single = CurrentDpiScale(form)
        If Math.Abs(scale - 1.0F) < 0.02F Then Return

        Dim dummy As Object = Nothing
        If Not scaledControls.TryGetValue(form, dummy) Then
            scaledControls.Add(form, Nothing)
            If form.WindowState <> FormWindowState.Maximized Then
                form.Size = ScaleSize(form.Size, scale)
            End If
            If form.MinimumSize.Width > 0 OrElse form.MinimumSize.Height > 0 Then
                form.MinimumSize = ScaleSize(form.MinimumSize, scale)
            End If
            If form.AutoScroll Then
                form.AutoScrollMinSize = ScaleSize(form.AutoScrollMinSize, scale)
            End If
        End If

        ScaleChildren(form, scale)
    End Sub

    Private Function CurrentDpiScale(form As Form) As Single
        Try
            Using graphics As Graphics = form.CreateGraphics()
                Return graphics.DpiX / 96.0F
            End Using
        Catch
            Return 1.0F
        End Try
    End Function

    Private Sub ScaleChildren(parent As Control, scale As Single)
        For Each child As Control In parent.Controls
            Dim dummy As Object = Nothing
            If Not scaledControls.TryGetValue(child, dummy) Then
                scaledControls.Add(child, Nothing)

                child.Location = ScalePoint(child.Location, scale)
                If child.AutoSize Then
                    ' Auto-sized controls (single-line TextBox, ...) drive their own
                    ' height, but a width that was set explicitly must still be
                    ' scaled - otherwise a 96-DPI field stays narrow next to a
                    ' scaled ComboBox and the form's fields no longer line up.
                    child.Width = CInt(Math.Round(child.Width * scale))
                Else
                    child.Size = ScaleSize(child.Size, scale)
                End If
                child.Padding = ScalePadding(child.Padding, scale)
                child.Margin = ScalePadding(child.Margin, scale)

                ' NOTE: font sizes are deliberately NOT scaled here. The app is
                ' DPI-aware, so fonts specified in points already render larger on
                ' high-DPI displays. Scaling them again would double-scale the text
                ' and clip it inside the scaled controls.

                Dim table As TableLayoutPanel = TryCast(child, TableLayoutPanel)
                If table IsNot Nothing Then
                    For Each column As ColumnStyle In table.ColumnStyles
                        If column.SizeType = SizeType.Absolute Then column.Width *= scale
                    Next
                    For Each row As RowStyle In table.RowStyles
                        If row.SizeType = SizeType.Absolute Then row.Height *= scale
                    Next
                End If

                Dim grid As DataGridView = TryCast(child, DataGridView)
                If grid IsNot Nothing Then
                    ' The report grids are deliberately rigid: their column widths
                    ' are fixed pixels rather than fill weights. Those widths must
                    ' still be scaled, otherwise the columns stop well short of the
                    ' grid's right edge on a high-DPI display.
                    For Each column As DataGridViewColumn In grid.Columns
                        If column.InheritedAutoSizeMode = DataGridViewAutoSizeColumnMode.None Then
                            column.Width = CInt(Math.Round(column.Width * scale))
                        End If
                    Next
                End If
            End If

            ScaleChildren(child, scale)
        Next
    End Sub

    Private Function ScalePoint(value As Point, scale As Single) As Point
        Return New Point(CInt(Math.Round(value.X * scale)), CInt(Math.Round(value.Y * scale)))
    End Function

    Private Function ScaleSize(value As Size, scale As Single) As Size
        Return New Size(
            Math.Max(0, CInt(Math.Round(value.Width * scale))),
            Math.Max(0, CInt(Math.Round(value.Height * scale))))
    End Function

    Private Function ScalePadding(value As Padding, scale As Single) As Padding
        Return New Padding(
            Math.Max(0, CInt(Math.Round(value.Left * scale))),
            Math.Max(0, CInt(Math.Round(value.Top * scale))),
            Math.Max(0, CInt(Math.Round(value.Right * scale))),
            Math.Max(0, CInt(Math.Round(value.Bottom * scale))))
    End Function

    Private Function Shade(color As Color, amount As Integer) As Color
        Return Color.FromArgb(color.A, ClampChannel(color.R + amount), ClampChannel(color.G + amount), ClampChannel(color.B + amount))
    End Function

    Private Function ClampChannel(value As Integer) As Integer
        Return Math.Max(0, Math.Min(255, value))
    End Function

    Private Function IsTransparent(color As Color) As Boolean
        Return color = Color.Transparent OrElse color.A < 255
    End Function

    ' ---- Grids ----------------------------------------------------------

    Public Sub StyleGrid(grid As DataGridView)
        grid.BackgroundColor = Surface
        grid.BorderStyle = BorderStyle.None
        grid.EnableHeadersVisualStyles = False
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        grid.ColumnHeadersHeight = 40
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt
        grid.ColumnHeadersDefaultCellStyle.ForeColor = MutedText
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceAlt
        grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        grid.DefaultCellStyle.BackColor = Surface
        grid.DefaultCellStyle.ForeColor = PrimaryText
        grid.DefaultCellStyle.SelectionBackColor = AccentSoft
        grid.DefaultCellStyle.SelectionForeColor = PrimaryText
        grid.DefaultCellStyle.Padding = New Padding(6, 2, 6, 2)
        grid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt
        grid.GridColor = Border
        grid.RowHeadersVisible = False
        grid.RowTemplate.Height = 30
        grid.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
    End Sub

    ' ---- Inputs ---------------------------------------------------------

    Public Function MakeTextBox() As TextBox
        Dim txt As New TextBox With {
            .BackColor = Color.White,
            .ForeColor = PrimaryText,
            .BorderStyle = BorderStyle.FixedSingle,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
        }
        txt.Height = 34
        Return txt
    End Function

    ' ---- Branding -------------------------------------------------------

    Private cachedLogo As Image = Nothing
    Private logoLoaded As Boolean = False
    Private cachedIcon As Icon = Nothing
    Private iconLoaded As Boolean = False

    ''' <summary>
    ''' Loads assets\logo.png next to the executable, or Nothing when the asset
    ''' is missing so every caller can fall back gracefully.
    ''' </summary>
    Public Function LoadLogo() As Image
        If logoLoaded Then Return cachedLogo
        logoLoaded = True
        Try
            Dim path As String = IO.Path.Combine(Application.StartupPath, "assets", "logo.png")
            If IO.File.Exists(path) Then
                ' Read through a stream and copy into an independent bitmap so the
                ' file is never locked and the image outlives the stream.
                Using stream As New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read)
                    Using loaded As Image = Image.FromStream(stream)
                        cachedLogo = New Bitmap(loaded)
                    End Using
                End Using
            End If
        Catch ex As Exception
            AppLogger.Error("Unable to load the application logo", ex)
        End Try
        Return cachedLogo
    End Function

    ''' <summary>Logo control sized to fit, or Nothing when the asset is missing.</summary>
    Public Function MakeLogo(size As Integer) As PictureBox
        Dim image As Image = LoadLogo()
        If image Is Nothing Then Return Nothing
        Return New PictureBox With {
            .Image = image,
            .SizeMode = PictureBoxSizeMode.Zoom,
            .Size = New Size(size, size),
            .BackColor = Color.Transparent,
            .TabStop = False
        }
    End Function

    Private Function LoadIcon() As Icon
        If iconLoaded Then Return cachedIcon
        iconLoaded = True
        Try
            Dim path As String = IO.Path.Combine(Application.StartupPath, "assets", "logo.ico")
            If IO.File.Exists(path) Then cachedIcon = New Icon(path)
        Catch ex As Exception
            AppLogger.Error("Unable to load the application icon", ex)
        End Try
        Return cachedIcon
    End Function

End Module
