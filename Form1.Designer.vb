<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Dashboard
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.btnAddPart = New System.Windows.Forms.Button()
        Me.btnIssuePart = New System.Windows.Forms.Button()
        Me.btnStockCard = New System.Windows.Forms.Button()
        Me.btnInventory = New System.Windows.Forms.Button()
        Me.btnPurchaseBatch = New System.Windows.Forms.Button()
        Me.btnSoldItem = New System.Windows.Forms.Button()
        Me.btnSupplier = New System.Windows.Forms.Button()
        Me.btnCustomer = New System.Windows.Forms.Button()
        Me.btnExit = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'btnAddPart
        '
        Me.btnAddPart.Location = New System.Drawing.Point(38, 46)
        Me.btnAddPart.Name = "btnAddPart"
        Me.btnAddPart.Size = New System.Drawing.Size(129, 38)
        Me.btnAddPart.TabIndex = 0
        Me.btnAddPart.Text = "Add Spare Part"
        Me.btnAddPart.UseVisualStyleBackColor = True
        '
        'btnIssuePart
        '
        Me.btnIssuePart.Location = New System.Drawing.Point(40, 101)
        Me.btnIssuePart.Name = "btnIssuePart"
        Me.btnIssuePart.Size = New System.Drawing.Size(128, 40)
        Me.btnIssuePart.TabIndex = 1
        Me.btnIssuePart.Text = "Issue Spare Part"
        Me.btnIssuePart.UseVisualStyleBackColor = True
        '
        'btnStockCard
        '
        Me.btnStockCard.Location = New System.Drawing.Point(38, 160)
        Me.btnStockCard.Name = "btnStockCard"
        Me.btnStockCard.Size = New System.Drawing.Size(130, 41)
        Me.btnStockCard.TabIndex = 2
        Me.btnStockCard.Text = "View Stock Card"
        Me.btnStockCard.UseVisualStyleBackColor = True
        '
        'btnInventory
        '
        Me.btnInventory.Location = New System.Drawing.Point(234, 46)
        Me.btnInventory.Name = "btnInventory"
        Me.btnInventory.Size = New System.Drawing.Size(136, 37)
        Me.btnInventory.TabIndex = 3
        Me.btnInventory.Text = "View Inventory"
        Me.btnInventory.UseVisualStyleBackColor = True
        '
        'btnPurchaseBatch
        '
        Me.btnPurchaseBatch.Location = New System.Drawing.Point(234, 101)
        Me.btnPurchaseBatch.Name = "btnPurchaseBatch"
        Me.btnPurchaseBatch.Size = New System.Drawing.Size(136, 39)
        Me.btnPurchaseBatch.TabIndex = 4
        Me.btnPurchaseBatch.Text = "View Purchase Batch"
        Me.btnPurchaseBatch.UseVisualStyleBackColor = True
        '
        'btnSoldItem
        '
        Me.btnSoldItem.Location = New System.Drawing.Point(234, 160)
        Me.btnSoldItem.Name = "btnSoldItem"
        Me.btnSoldItem.Size = New System.Drawing.Size(136, 41)
        Me.btnSoldItem.TabIndex = 5
        Me.btnSoldItem.Text = "View Sold Item"
        Me.btnSoldItem.UseVisualStyleBackColor = True
        '
        'btnSupplier
        '
        Me.btnSupplier.Location = New System.Drawing.Point(446, 46)
        Me.btnSupplier.Name = "btnSupplier"
        Me.btnSupplier.Size = New System.Drawing.Size(140, 38)
        Me.btnSupplier.TabIndex = 6
        Me.btnSupplier.Text = "Supplier Information"
        Me.btnSupplier.UseVisualStyleBackColor = True
        '
        'btnCustomer
        '
        Me.btnCustomer.Location = New System.Drawing.Point(449, 101)
        Me.btnCustomer.Name = "btnCustomer"
        Me.btnCustomer.Size = New System.Drawing.Size(136, 40)
        Me.btnCustomer.TabIndex = 7
        Me.btnCustomer.Text = "Customer Information"
        Me.btnCustomer.UseVisualStyleBackColor = True
        '
        'btnExit
        '
        Me.btnExit.Location = New System.Drawing.Point(451, 160)
        Me.btnExit.Name = "btnExit"
        Me.btnExit.Size = New System.Drawing.Size(134, 41)
        Me.btnExit.TabIndex = 8
        Me.btnExit.Text = "Exit"
        Me.btnExit.UseVisualStyleBackColor = True
        '
        'Dashboard
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1180, 820)
        Me.Controls.Add(Me.btnExit)
        Me.Controls.Add(Me.btnCustomer)
        Me.Controls.Add(Me.btnSupplier)
        Me.Controls.Add(Me.btnSoldItem)
        Me.Controls.Add(Me.btnPurchaseBatch)
        Me.Controls.Add(Me.btnInventory)
        Me.Controls.Add(Me.btnStockCard)
        Me.Controls.Add(Me.btnIssuePart)
        Me.Controls.Add(Me.btnAddPart)
        Me.Name = "Dashboard"
        Me.Text = "MIOS Inventory Management"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents btnAddPart As Button
    Friend WithEvents btnIssuePart As Button
    Friend WithEvents btnStockCard As Button
    Friend WithEvents btnInventory As Button
    Friend WithEvents btnPurchaseBatch As Button
    Friend WithEvents btnSoldItem As Button
    Friend WithEvents btnSupplier As Button
    Friend WithEvents btnCustomer As Button
    Friend WithEvents btnExit As Button
End Class
