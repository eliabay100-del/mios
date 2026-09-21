Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class Dashboard
    Public Property UserID As Integer = 0
    Public Property Username As String = ""

    Private pnlHeader As Panel
    Private pnlQuickActions As Panel
    Private lblTitle As Label

    Public Sub New()
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "MIOS Inventory Management"
        Me.Size = New Size(1180, 820)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.None
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ControlBox = False

        InitializeDashboard()
    End Sub

    Private Sub InitializeDashboard()
        pnlHeader = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 140,
            .BackColor = ModernTheme.Surface
        }

        lblTitle = New Label With {
            .Text = "Inventory Management",
            .Font = New Font("Segoe UI", 28, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .AutoSize = True,
            .Location = New Point(28, 48)
        }
        pnlHeader.Controls.Add(lblTitle)

        Dim subtitle As New Label With {
            .Text = "Operations dashboard",
            .ForeColor = ModernTheme.MutedText,
            .Font = New Font("Segoe UI", 11.0F),
            .AutoSize = True,
            .Location = New Point(30, 94)
        }
        pnlHeader.Controls.Add(subtitle)

        Dim userChip As New Panel With {
            .Size = New Size(168, 42),
            .Location = New Point(Me.Width - 208, 48),
            .BackColor = ModernTheme.SurfaceAlt,
            .BorderStyle = BorderStyle.None
        }
        Dim userText As New Label With {
            .Text = If(String.IsNullOrEmpty(Username), "Admin User", Username),
            .ForeColor = ModernTheme.PrimaryText,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(18, 11)
        }
        userChip.Controls.Add(userText)
        pnlHeader.Controls.Add(userChip)
        Me.Controls.Add(pnlHeader)

        pnlQuickActions = New Panel With {
            .Location = New Point(30, 170),
            .Size = New Size(1100, 600),
            .BackColor = Color.Transparent
        }

        Dim hero As New Panel With {
            .Location = New Point(0, 0),
            .Size = New Size(1040, 170),
            .BackColor = ModernTheme.SurfaceAlt
        }
        Dim heroText As New Label With {
            .Text = "Your inventory, finally in sync.",
            .ForeColor = ModernTheme.PrimaryText,
            .Font = New Font("Segoe UI", 26, FontStyle.Bold),
            .Location = New Point(26, 28),
            .AutoSize = True
        }
        hero.Controls.Add(heroText)
        Dim heroSub As New Label With {
            .Text = "Track stock movement, purchasing, customers, and suppliers from one dashboard.",
            .ForeColor = ModernTheme.MutedText,
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
            .Location = New Point(0, 338),
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

        Dim btnExitUI As Button = ModernTheme.MakeSecondaryButton("Exit System")
        btnExitUI.Location = New Point(930, 560)
        btnExitUI.Width = 100
        btnExitUI.Height = 38
        btnExitUI.ForeColor = ModernTheme.Danger
        AddHandler btnExitUI.Click, AddressOf btnExit_Click
        pnlQuickActions.Controls.Add(btnExitUI)
    End Sub

    Private Structure DashboardStats
        Public StockOnHand As Integer
        Public PurchaseBatches As Integer
        Public LowInventory As Integer
        Public SalesToday As Decimal
    End Structure

    Private Function GetDashboardStats() As DashboardStats
        Dim stats As New DashboardStats()
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                stats.StockOnHand = Convert.ToInt32(ExecuteScalar(conn, "SELECT ISNULL(SUM(Quantity), 0) FROM mn.SpareParts"))
                stats.PurchaseBatches = Convert.ToInt32(ExecuteScalar(conn, "SELECT COUNT(*) FROM mn.PurchaseBatch"))
                stats.LowInventory = Convert.ToInt32(ExecuteScalar(conn, "SELECT COUNT(*) FROM mn.SpareParts WHERE Quantity <= 5"))
                stats.SalesToday = Convert.ToDecimal(ExecuteScalar(conn, "SELECT ISNULL(SUM(ii.QuantityIssued * sp.UnitPrice), 0) FROM mn.IssuedItem ii JOIN mn.SpareParts sp ON sp.PartNumber = ii.PartNumber WHERE CAST(ii.IssueDate AS DATE) = CAST(GETDATE() AS DATE)"))
            End Using
        Catch ex As Exception
            MessageBox.Show("Unable to load dashboard statistics: " & ex.Message, "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        Return stats
    End Function

    Private Function ExecuteScalar(conn As SqlConnection, query As String) As Object
        Using cmd As New SqlCommand(query, conn)
            Return cmd.ExecuteScalar()
        End Using
    End Function

    Private Function CreateMenuButton(text As String, handler As EventHandler) As Button
        Dim btn As Button = ModernTheme.MakePrimaryButton(text)
        btn.Width = 180
        btn.Height = 54
        btn.BackColor = ModernTheme.Surface
        btn.ForeColor = ModernTheme.PrimaryText
        btn.FlatAppearance.BorderColor = ModernTheme.Border
        btn.FlatAppearance.BorderSize = 1
        btn.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        AddHandler btn.Click, handler
        Return btn
    End Function

    Private Function CreateStatCard(title As String, value As String, note As String) As Panel
        Dim card As New Panel With {
            .BackColor = ModernTheme.Surface,
            .Padding = New Padding(16),
            .Height = 110,
            .Margin = New Padding(8)
        }

        Dim lblTitle As New Label With {
            .Text = title,
            .ForeColor = ModernTheme.MutedText,
            .Font = New Font("Segoe UI", 10.0F),
            .Location = New Point(18, 18),
            .AutoSize = True
        }
        card.Controls.Add(lblTitle)

        Dim lblValue As New Label With {
            .Text = value,
            .ForeColor = ModernTheme.PrimaryText,
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .Location = New Point(18, 42),
            .AutoSize = True
        }
        card.Controls.Add(lblValue)

        Dim lblNote As New Label With {
            .Text = note,
            .ForeColor = ModernTheme.Success,
            .Font = New Font("Segoe UI", 9.0F),
            .Location = New Point(18, 82),
            .AutoSize = True
        }
        card.Controls.Add(lblNote)
        Return card
    End Function

    Private Sub btnAddPart_Click(sender As Object, e As EventArgs)
        Dim frm As New Add_SparePart() : frm.ShowDialog()
    End Sub

    Private Sub btnIssuePart_Click(sender As Object, e As EventArgs)
        Dim frm As New Issue_SparePart() : frm.ShowDialog()
    End Sub

    Private Sub btnStockCard_Click(sender As Object, e As EventArgs)
        Dim frm As New View_StockCard() : frm.ShowDialog()
    End Sub

    Private Sub btnInventory_Click(sender As Object, e As EventArgs)
        Dim frm As New View_Inventory() : frm.ShowDialog()
    End Sub

    Private Sub btnPurchaseBatch_Click(sender As Object, e As EventArgs)
        Dim frm As New View_PurchaseBatch() : frm.ShowDialog()
    End Sub

    Private Sub btnSoldItem_Click(sender As Object, e As EventArgs)
        Dim frm As New View_SoldItem() : frm.ShowDialog()
    End Sub

    Private Sub btnSupplier_Click(sender As Object, e As EventArgs)
        Dim frm As New Supplier() : frm.ShowDialog()
    End Sub

    Private Sub btnCustomer_Click(sender As Object, e As EventArgs)
        Dim frm As New Customer() : frm.ShowDialog()
    End Sub

    Private Sub btnExit_Click(sender As Object, e As EventArgs)
        If MessageBox.Show("Are you sure you want to exit?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Application.Exit()
        End If
    End Sub
End Class
