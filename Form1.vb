Imports System.Data.SqlClient
Imports System.Drawing

Public Class Dashboard
    ' Properties for user context (kept in code logic but removed from UI display)
    Public Property UserID As Integer = 0
    Public Property Username As String = ""

    ' UI Controls
    Private pnlHeader As Panel
    Private pnlQuickActions As Panel
    Private lblTitle As Label

    Public Sub New()
        ' Form Setup
        Me.Text = "MIOS Inventory Management"
        Me.Size = New Size(960, 600)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(244, 246, 248)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False

        InitializeDashboard()
    End Sub

    Private Sub InitializeDashboard()
        ' 1. Header Section (Only Title)
        pnlHeader = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 100,
            .BackColor = Color.FromArgb(21, 101, 192)
        }

        lblTitle = New Label With {
            .Text = "MIOS INVENTORY MANAGEMENT SYSTEM",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = False,
            .Size = New Size(960, 100),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        pnlHeader.Controls.Add(lblTitle)
        Me.Controls.Add(pnlHeader)

        ' 2. Actions Section (Only Buttons)
        pnlQuickActions = New Panel With {
            .Location = New Point(20, 120),
            .Size = New Size(900, 420),
            .BackColor = Color.Transparent
        }

        ' Row 1
        CreateMenuButton("Add Spare Part", 30, 20, AddressOf btnAddPart_Click)
        CreateMenuButton("Issue Spare Part", 310, 20, AddressOf btnIssuePart_Click)
        CreateMenuButton("View Stock Card", 590, 20, AddressOf btnStockCard_Click)

        ' Row 2
        CreateMenuButton("Inventory List", 30, 120, AddressOf btnInventory_Click)
        CreateMenuButton("Purchase Batches", 310, 120, AddressOf btnPurchaseBatch_Click)
        CreateMenuButton("Sold Items", 590, 120, AddressOf btnSoldItem_Click)

        ' Row 3
        CreateMenuButton("Manage Suppliers", 30, 220, AddressOf btnSupplier_Click)
        CreateMenuButton("Manage Customers", 310, 220, AddressOf btnCustomer_Click)

        ' Exit Button
        Dim btnExitUI = CreateMenuButton("Exit System", 590, 220, AddressOf btnExit_Click)
        btnExitUI.BackColor = Color.White
        btnExitUI.ForeColor = Color.Crimson
        btnExitUI.FlatAppearance.BorderColor = Color.Crimson

        Me.Controls.Add(pnlQuickActions)
    End Sub

    Private Function CreateMenuButton(text As String, x As Integer, y As Integer, handler As EventHandler) As Button
        Dim btn As New Button With {
            .Text = text,
            .Location = New Point(x, y),
            .Size = New Size(260, 80),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .BackColor = Color.White,
            .ForeColor = Color.FromArgb(33, 33, 33),
            .Cursor = Cursors.Hand
        }
        btn.FlatAppearance.BorderSize = 1
        btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200)

        AddHandler btn.Click, handler
        pnlQuickActions.Controls.Add(btn)
        Return btn
    End Function

    ' ============================
    ' EVENT HANDLERS
    ' ============================

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