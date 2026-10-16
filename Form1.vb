Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class Dashboard
    Public Property UserID As Integer = 0
    Public Property Username As String = ""

    Private pnlHeader As Panel
    Private pnlQuickActions As Panel
    Private pnlAdminActions As FlowLayoutPanel
    Private lblTitle As Label
    Private btnLanguage As Button
    Private ReadOnly statValueLabels As New List(Of Label)()

    Public Sub New()
        InitializeComponent()
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "MIOS Inventory Management"
        Me.Size = New Size(1180, 820)
        Me.MinimumSize = New Size(980, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = False
        Me.ControlBox = True
        Me.WindowState = FormWindowState.Maximized

        InitializeDashboard()
        ModernTheme.EnableVerticalScroll(Me, 1000)

        ' On small screens the dashboard scrolls. Windows Forms also scrolls the
        ' initially focused control into view, which used to push the header (and
        ' the logo) off the top, so pin the view to the top once the form settles.
        AddHandler Me.Shown, Sub(s, e)
                                 ResetScrollToTop()
                                 Dim settle As New System.Windows.Forms.Timer With {.Interval = 250}
                                 AddHandler settle.Tick, Sub(ts, te)
                                                             settle.Stop()
                                                             settle.Dispose()
                                                             ResetScrollToTop()
                                                         End Sub
                                 settle.Start()
                             End Sub
    End Sub

    ''' <summary>Scrolls the dashboard back to the very top (header/logo visible).</summary>
    Private Sub ResetScrollToTop()
        If AutoScrollPosition.Y <> 0 Then AutoScrollPosition = New Point(0, 0)
    End Sub

    Private Sub InitializeDashboard()
        pnlHeader = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 140,
            .TabIndex = 0,
            .BackColor = Color.FromArgb(255, 255, 255)
        }

        Dim logo As PictureBox = ModernTheme.MakeLogo(92)
        If logo IsNot Nothing Then
            logo.Location = New Point(28, 24)
            logo.BackColor = Color.White
            pnlHeader.Controls.Add(logo)
        End If
        Dim contentLeft As Integer = If(logo IsNot Nothing, 138, 28)

        lblTitle = New Label With {
            .Text = "Inventory Management",
            .Font = New Font("Segoe UI", 28, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .AutoSize = True,
            .Location = New Point(contentLeft, 40)
        }
        pnlHeader.Controls.Add(lblTitle)

        Dim subtitle As New Label With {
            .Text = "Operations dashboard",
            .ForeColor = ModernTheme.MutedText,
            .Font = New Font("Segoe UI", 11.0F),
            .AutoSize = True,
            .Location = New Point(contentLeft + 2, 88)
        }
        pnlHeader.Controls.Add(subtitle)

        Dim btnHeaderExit As Button = ModernTheme.MakeDangerButton("Exit")
        btnHeaderExit.Size = New Size(96, 42)
        btnHeaderExit.Tag = "HeaderExit"
        AddHandler btnHeaderExit.Click, AddressOf btnExit_Click

        btnLanguage = ModernTheme.MakeSecondaryButton("አማርኛ")
        btnLanguage.Size = New Size(110, 42)
        AddHandler btnLanguage.Click, Sub(sender, e)
                                        LanguageService.Toggle()
                                        btnLanguage.Text = If(LanguageService.IsAmharic, "English", "አማርኛ")
                                    End Sub

        ' The user chip is a button that opens account and administrator actions.
        ' SecurityContext is the source of truth: the Dashboard's own Username
        ' property is assigned by the caller after this constructor has run.
        Dim displayUser As String = SecurityContext.CurrentUsername
        If String.IsNullOrEmpty(displayUser) Then displayUser = Username
        If String.IsNullOrEmpty(displayUser) Then displayUser = "Admin User"
        Dim displayRole As String = SecurityContext.CurrentRole
        If String.IsNullOrEmpty(displayRole) Then displayRole = "User"

        Dim btnUserMenu As Button = ModernTheme.MakeSecondaryButton(
            displayUser & "  /  " & displayRole & "  ▾")
        btnUserMenu.AutoSize = False
        btnUserMenu.Size = New Size(236, 42)
        btnUserMenu.Margin = New Padding(8, 3, 8, 3)

        Dim userMenu As New ContextMenuStrip With {.Font = New Font("Segoe UI", 9.5F)}
        Dim miUsers As New ToolStripMenuItem("Users && Roles")
        miUsers.Visible = SecurityContext.HasPermission("USER_MANAGE")
        AddHandler miUsers.Click, Sub(sender, e) RunFeature(Sub() OpenDialog(Function() New UserManagementForm()))
        Dim miPassword As New ToolStripMenuItem("Change my password")
        AddHandler miPassword.Click, Sub(sender, e) RunFeature(Sub() OpenDialog(Function() New ChangePasswordForm()))
        Dim miBackup As New ToolStripMenuItem("Database backup")
        miBackup.Visible = SecurityContext.HasRole("Admin")
        AddHandler miBackup.Click, Sub(sender, e) RunFeature(Sub() OpenDialog(Function() New BackupForm()))
        userMenu.Items.Add(miUsers)
        userMenu.Items.Add(miPassword)
        userMenu.Items.Add(New ToolStripSeparator())
        userMenu.Items.Add(miBackup)
        AddHandler btnUserMenu.Click, Sub(sender, e) userMenu.Show(btnUserMenu, 0, btnUserMenu.Height)

        ' Right-aligned header group so the actions stay pinned to the right edge
        ' no matter how wide the (maximized) window is.
        Dim pnlHeaderRight As New FlowLayoutPanel With {
            .Dock = DockStyle.Right,
            .Width = 660,
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False,
            .BackColor = Color.Transparent,
            .Padding = New Padding(0, 46, 20, 0)
        }
        pnlHeaderRight.Controls.Add(btnHeaderExit)
        pnlHeaderRight.Controls.Add(btnLanguage)
        pnlHeaderRight.Controls.Add(btnUserMenu)
        pnlHeader.Controls.Add(pnlHeaderRight)
        Me.Controls.Add(pnlHeader)

        pnlQuickActions = New Panel With {
            .Location = New Point(30, 170),
            .Size = New Size(1100, 790),
            .TabIndex = 1,
            .BackColor = Color.Transparent
        }

        Dim hero As New Panel With {
            .Location = New Point(0, 0),
            .Size = New Size(1040, 170),
            .BackColor = Color.FromArgb(28, 39, 44)
        }
        Dim heroText As New Label With {
            .Text = "Your inventory, finally in sync.",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 26, FontStyle.Bold),
            .Location = New Point(26, 28),
            .AutoSize = True
        }
        hero.Controls.Add(heroText)
        Dim heroSub As New Label With {
            .Text = "Track stock movement, purchasing, customers, and suppliers from one dashboard.",
            .ForeColor = Color.FromArgb(210, 210, 210),
            .Font = New Font("Segoe UI", 11.0F),
            .Location = New Point(28, 80),
            .Size = New Size(500, 50)
        }
        hero.Controls.Add(heroSub)
        pnlQuickActions.Controls.Add(hero)

        Dim actionPanel As New FlowLayoutPanel With {
            .Location = New Point(0, 190),
            .Size = New Size(1040, 150),
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .BackColor = Color.Transparent
        }

        Dim buttons As New List(Of Button) From {
            CreateMenuButton("Add Spare Part", AddressOf btnAddPart_Click),
            CreateMenuButton("Issue Spare Part", AddressOf btnIssuePart_Click),
            CreateMenuButton("Inventory List", AddressOf btnInventory_Click),
            CreateMenuButton("Purchase Batches", AddressOf btnPurchaseBatch_Click),
            CreateMenuButton("Sold Items", AddressOf btnSoldItem_Click),
            CreateMenuButton("Stock Card", AddressOf btnStockCard_Click),
            CreateMenuButton("Suppliers", AddressOf btnSupplier_Click),
            CreateMenuButton("Customers", AddressOf btnCustomer_Click)
        }

        For Each btn As Button In buttons
            btn.Width = 160
            btn.Height = 46
            btn.Margin = New Padding(8)
            actionPanel.Controls.Add(btn)
        Next

        pnlQuickActions.Controls.Add(actionPanel)

        Dim statGrid As New TableLayoutPanel With {
            .Location = New Point(0, 358),
            .Size = New Size(1040, 260),
            .ColumnCount = 2,
            .RowCount = 2,
            .BackColor = Color.Transparent,
            .Padding = New Padding(0, 14, 0, 0)
        }
        statGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))
        statGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))
        statGrid.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0!))
        statGrid.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0!))

        Dim stats = GetDashboardStats()
        statGrid.Controls.Add(CreateStatCard("Stock on hand", stats.StockOnHand.ToString("N0"), "Current quantity"), 0, 0)
        statGrid.Controls.Add(CreateStatCard("Purchase batches", stats.PurchaseBatches.ToString("N0"), "Recorded batches"), 1, 0)
        statGrid.Controls.Add(CreateStatCard("Low inventory", stats.LowInventory.ToString("N0"), "Parts at or below 5"), 0, 1)
        statGrid.Controls.Add(CreateStatCard("Sales today", stats.SalesToday.ToString("N2"), "Current price total"), 1, 1)

        pnlQuickActions.Controls.Add(statGrid)
        Me.Controls.Add(pnlQuickActions)

        pnlAdminActions = New FlowLayoutPanel With {
            .Location = New Point(0, 640),
            .Size = New Size(1040, 135),
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .BackColor = Color.Transparent,
            .Padding = New Padding(0, 8, 0, 0)
        }
        pnlQuickActions.Controls.Add(pnlAdminActions)
        AddHandler Me.Resize, Sub() CenterDashboardContent()
        ' Refresh the stat cards whenever the dashboard is re-activated (for
        ' example after receiving or issuing stock), so they never go stale.
        AddHandler Me.Activated, Sub(sender, e) RefreshStats()

        AddDashboardAction("Record Payment", 0, 0, SecurityContext.HasPermission("PAYMENT_MANAGE"), Sub()
            OpenDialog(Function() New PaymentForm())
        End Sub)

        AddDashboardAction("Users & Roles", 30, 610, SecurityContext.HasPermission("USER_MANAGE"), Sub()
            Using form As New UserManagementForm()
                form.ShowDialog()
            End Using
        End Sub)
        AddDashboardAction("Manage Categories", 30, 610, SecurityContext.HasRole("Admin"), Sub()
            OpenDialog(Function() New CategoryManagementForm())
        End Sub)
        AddDashboardAction("Adjust Stock", 185, 610, SecurityContext.HasPermission("INVENTORY_ADJUST"), Sub()
            Using form As New InventoryAdjustmentForm()
                form.ShowDialog()
            End Using
        End Sub)
        AddDashboardAction("Financial Reports", 340, 610, SecurityContext.HasPermission("REPORT_VIEW"), Sub()
            Using form As New FinancialReportsForm()
                form.ShowDialog()
            End Using
        End Sub)
        AddDashboardAction("Record Expense", 495, 610, SecurityContext.HasPermission("EXPENSE_MANAGE"), Sub()
            Using form As New ExpenseForm()
                form.ShowDialog()
            End Using
        End Sub)
        AddDashboardAction("Process Return", 650, 610, SecurityContext.HasPermission("RETURN_MANAGE"), Sub()
            Using form As New ReturnForm()
                form.ShowDialog()
            End Using
        End Sub)
        AddDashboardAction("Database Backup", 805, 610, SecurityContext.HasRole("Admin"), Sub()
            OpenDialog(Function() New BackupForm())
        End Sub)

    End Sub

    Private Sub CenterDashboardContent()
        If pnlQuickActions Is Nothing Then Return
        pnlQuickActions.Left = Math.Max(24, (Me.ClientSize.Width - pnlQuickActions.Width) \ 2)
    End Sub

    Private Sub AddDashboardAction(text As String, x As Integer, y As Integer, visible As Boolean, action As Action)
        Dim button As Button = ModernTheme.MakeSecondaryButton(text)
        button.Width = 180
        button.Height = 42
        button.AutoEllipsis = False
        button.Padding = New Padding(8, 4, 8, 4)
        button.Tag = "AdminAction"
        button.BackColor = Color.FromArgb(54, 69, 79)
        button.ForeColor = Color.White
        button.FlatAppearance.BorderColor = Color.FromArgb(54, 69, 79)
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 87, 98)
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(42, 54, 62)
        button.Visible = visible
        AddHandler button.Click, Sub(sender, e) RunFeature(action)
        pnlAdminActions.Controls.Add(button)
    End Sub

    Private Sub RunFeature(action As Action)
        Try
            action()
        Catch ex As UnauthorizedAccessException
            MessageBox.Show(ex.Message, "Access denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Catch ex As Exception
            AppLogger.Error("Unable to open dashboard feature", ex)
            MessageBox.Show("Unable to open this feature: " & ex.Message, "MIOS", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OpenDialog(factory As Func(Of Form))
        Using form As Form = factory()
            form.ShowDialog(Me)
        End Using
    End Sub

    Private Structure DashboardStats
        Public StockOnHand As Integer
        Public PurchaseBatches As Integer
        Public LowInventory As Integer
        Public SalesToday As Decimal
    End Structure

    Private Function GetDashboardStats(Optional suppressError As Boolean = False) As DashboardStats
        Dim stats As New DashboardStats()
        Me.Cursor = Cursors.WaitCursor
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                stats.StockOnHand = Convert.ToInt32(ExecuteScalar(conn, "SELECT ISNULL(SUM(Quantity), 0) FROM mn.SpareParts"))
                stats.PurchaseBatches = Convert.ToInt32(ExecuteScalar(conn, "SELECT COUNT(*) FROM mn.PurchaseBatch"))
                stats.LowInventory = Convert.ToInt32(ExecuteScalar(conn, "SELECT COUNT(*) FROM mn.SpareParts WHERE Quantity <= ReorderLevel"))
                stats.SalesToday = Convert.ToDecimal(ExecuteScalar(conn, "SELECT ISNULL(SUM(TotalAmount), 0) FROM mn.SalesInvoice WHERE CAST(InvoiceDate AS DATE) = CAST(GETDATE() AS DATE)"))
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load dashboard statistics", ex)
            If Not suppressError Then
                MessageBox.Show("Unable to load dashboard statistics: " & ex.Message, "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Finally
            Me.Cursor = Cursors.Default
        End Try
        Return stats
    End Function

    ''' <summary>Updates the four stat cards without rebuilding the dashboard.</summary>
    Private Sub RefreshStats()
        If statValueLabels.Count <> 4 Then Return
        Dim stats As DashboardStats = GetDashboardStats(suppressError:=True)
        statValueLabels(0).Text = stats.StockOnHand.ToString("N0")
        statValueLabels(1).Text = stats.PurchaseBatches.ToString("N0")
        statValueLabels(2).Text = stats.LowInventory.ToString("N0")
        statValueLabels(3).Text = stats.SalesToday.ToString("N2")
    End Sub

    Private Function ExecuteScalar(conn As SqlConnection, query As String) As Object
        Using cmd As New SqlCommand(query, conn)
            Return cmd.ExecuteScalar()
        End Using
    End Function

    Private Function CreateMenuButton(text As String, handler As EventHandler) As Button
        Dim btn As Button = ModernTheme.MakePrimaryButton(text)
        btn.Width = 180
        btn.Height = 54
        btn.BackColor = Color.White
        btn.ForeColor = ModernTheme.PrimaryText
        btn.FlatAppearance.BorderColor = ModernTheme.Border
        btn.FlatAppearance.BorderSize = 1
        btn.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        AddHandler btn.Click, handler
        Return btn
    End Function

    Private Function CreateStatCard(title As String, value As String, note As String) As Panel
        Dim card As New Panel With {
            .BackColor = Color.White,
            .Padding = New Padding(16),
            .Margin = New Padding(8),
            .Dock = DockStyle.Fill
        }

        ' A 3-row layout (two auto rows + a filler row) keeps the footer text
        ' inside the card at any DPI instead of relying on tight fixed offsets.
        Dim layout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Transparent,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = New Padding(0)
        }
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0!))
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0!))

        Dim lblTitle As New Label With {
            .Text = title,
            .ForeColor = ModernTheme.MutedText,
            .Font = New Font("Segoe UI", 10.0F),
            .AutoSize = True,
            .Margin = New Padding(0)
        }

        Dim lblValue As New Label With {
            .Text = value,
            .ForeColor = ModernTheme.PrimaryText,
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .AutoSize = True,
            .Margin = New Padding(0, 2, 0, 0)
        }
        statValueLabels.Add(lblValue)

        Dim lblNote As New Label With {
            .Text = note,
            .ForeColor = ModernTheme.Success,
            .Font = New Font("Segoe UI", 9.0F),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.BottomLeft,
            .Margin = New Padding(0)
        }

        layout.Controls.Add(lblTitle, 0, 0)
        layout.Controls.Add(lblValue, 0, 1)
        layout.Controls.Add(lblNote, 0, 2)
        card.Controls.Add(layout)
        Return card
    End Function

    Private Sub btnAddPart_Click(sender As Object, e As EventArgs)
        OpenDialog(Function() New Add_SparePart())
    End Sub

    Private Sub btnIssuePart_Click(sender As Object, e As EventArgs)
        OpenDialog(Function() New Issue_SparePart())
    End Sub

    Private Sub btnStockCard_Click(sender As Object, e As EventArgs)
        OpenDialog(Function() New View_StockCard())
    End Sub

    Private Sub btnInventory_Click(sender As Object, e As EventArgs)
        OpenDialog(Function() New View_Inventory())
    End Sub

    Private Sub btnPurchaseBatch_Click(sender As Object, e As EventArgs)
        OpenDialog(Function() New View_PurchaseBatch())
    End Sub

    Private Sub btnSoldItem_Click(sender As Object, e As EventArgs)
        OpenDialog(Function() New View_SoldItem())
    End Sub

    Private Sub btnSupplier_Click(sender As Object, e As EventArgs)
        OpenDialog(Function() New Supplier())
    End Sub

    Private Sub btnCustomer_Click(sender As Object, e As EventArgs)
        OpenDialog(Function() New Customer())
    End Sub

    Private Sub btnExit_Click(sender As Object, e As EventArgs)
        If MessageBox.Show("Are you sure you want to exit?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Application.Exit()
        End If
    End Sub
End Class
