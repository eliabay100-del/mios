Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class InventoryAdjustmentForm
    Inherits Form

    Private ReadOnly cmbPart As New ComboBox()
    Private ReadOnly txtNewQuantity As New TextBox()
    Private ReadOnly txtReason As New TextBox()

    Public Sub New()
        If Not SecurityContext.HasPermission("INVENTORY_ADJUST") Then Throw New UnauthorizedAccessException("Your role cannot adjust inventory.")
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Inventory adjustment"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.WindowState = FormWindowState.Maximized
        Me.AutoScroll = True
        Me.Controls.Add(New Label With {.Text = "Inventory adjustment", .Font = New Font("Segoe UI", 22, FontStyle.Bold), .Location = New Point(28, 24), .AutoSize = True})
        AddField("Part", cmbPart, 90)
        AddField("New quantity", txtNewQuantity, 140)
        AddField("Reason", txtReason, 190)
        txtReason.Height = 55
        txtReason.Multiline = True
        cmbPart.DropDownStyle = ComboBoxStyle.DropDownList
        LoadParts()
        Dim btnSave As Button = ModernTheme.MakePrimaryButton("Save adjustment")
        btnSave.Location = New Point(345, 305)
        btnSave.Width = 160
        AddHandler btnSave.Click, AddressOf SaveAdjustment_Click
        Me.Controls.Add(btnSave)

        Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("Cancel")
        btnCancel.Location = New Point(180, 305)
        btnCancel.Width = 150
        AddHandler btnCancel.Click, Sub() Me.Close()
        Me.Controls.Add(btnCancel)

        Me.AcceptButton = btnSave
        Me.CancelButton = btnCancel
    End Sub

    Private Sub AddField(labelText As String, control As Control, top As Integer)
        Me.Controls.Add(New Label With {.Text = labelText, .Location = New Point(28, top + 5), .Size = New Size(120, 24), .ForeColor = ModernTheme.MutedText})
        control.Location = New Point(160, top)
        control.Width = 350
        Me.Controls.Add(control)
    End Sub

    Private Sub LoadParts()
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using cmd As New SqlCommand("SELECT PartNumber FROM mn.SpareParts ORDER BY PartNumber", conn)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            cmbPart.Items.Add(reader.GetString(0))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load parts for adjustment", ex)
            MessageBox.Show("Unable to load parts: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        If cmbPart.Items.Count > 0 Then cmbPart.SelectedIndex = 0
    End Sub

    Private Sub SaveAdjustment_Click(sender As Object, e As EventArgs)
        Dim newQuantity As Integer
        If cmbPart.SelectedIndex < 0 OrElse Not Integer.TryParse(txtNewQuantity.Text, newQuantity) OrElse newQuantity < 0 OrElse String.IsNullOrWhiteSpace(txtReason.Text) Then
            MessageBox.Show("Select a part and enter a non-negative quantity and reason.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Dim previousQuantity As Integer
                    Dim unitCost As Decimal
                    Using cmd As New SqlCommand("SELECT Quantity, UnitPrice FROM mn.SpareParts WITH (UPDLOCK, ROWLOCK) WHERE PartNumber = @part", conn, transaction)
                        cmd.Parameters.AddWithValue("@part", cmbPart.Text)
                        Using reader = cmd.ExecuteReader()
                            If Not reader.Read() Then Throw New Exception("Part was not found.")
                            previousQuantity = Convert.ToInt32(reader("Quantity"))
                            unitCost = Convert.ToDecimal(reader("UnitPrice"))
                        End Using
                    End Using

                    ' Value any stock added by an adjustment at the part's last
                    ' purchase cost rather than its selling price; otherwise the
                    ' inventory asset and later COGS are overstated.
                    Dim newBatchCost As Decimal = unitCost
                    Using cmd As New SqlCommand("SELECT TOP 1 UnitCost FROM mn.PurchaseBatch WHERE PartNumber = @part ORDER BY PurchaseDate DESC, BatchID DESC", conn, transaction)
                        cmd.Parameters.AddWithValue("@part", cmbPart.Text)
                        Dim costValue As Object = cmd.ExecuteScalar()
                        If costValue IsNot Nothing AndAlso costValue IsNot DBNull.Value Then newBatchCost = Convert.ToDecimal(costValue)
                    End Using

                    Dim difference As Integer = newQuantity - previousQuantity
                    If difference <> 0 Then
                        Dim direction As String = If(difference > 0, "increase", "decrease")
                        Dim answer As DialogResult = MessageBox.Show(
                            $"This will {direction} the stock of {cmbPart.Text} by {Math.Abs(difference)} (from {previousQuantity} to {newQuantity}). Continue?",
                            "Confirm adjustment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                        If answer <> DialogResult.Yes Then Return
                    End If

                    Dim adjustmentID As Integer
                    Using cmd As New SqlCommand("INSERT INTO mn.InventoryAdjustment (PartNumber, PreviousQuantity, NewQuantity, Difference, Reason, CreatedByUserID) OUTPUT INSERTED.AdjustmentID VALUES (@part, @previous, @new, @difference, @reason, @userID)", conn, transaction)
                        cmd.Parameters.AddWithValue("@part", cmbPart.Text)
                        cmd.Parameters.AddWithValue("@previous", previousQuantity)
                        cmd.Parameters.AddWithValue("@new", newQuantity)
                        cmd.Parameters.AddWithValue("@difference", difference)
                        cmd.Parameters.AddWithValue("@reason", txtReason.Text.Trim())
                        cmd.Parameters.AddWithValue("@userID", If(SecurityContext.CurrentUserID = 0, DBNull.Value, CObj(SecurityContext.CurrentUserID)))
                        adjustmentID = Convert.ToInt32(cmd.ExecuteScalar())
                    End Using
                    Using cmd As New SqlCommand("UPDATE mn.SpareParts SET Quantity = @quantity WHERE PartNumber = @part", conn, transaction)
                        cmd.Parameters.AddWithValue("@quantity", newQuantity)
                        cmd.Parameters.AddWithValue("@part", cmbPart.Text)
                        cmd.ExecuteNonQuery()
                    End Using

                    ' Keep the purchase-batch ledger aligned with the master quantity.
                    InventoryService.ApplyAdjustment(conn, transaction, cmbPart.Text, difference, newBatchCost)
                    If difference <> 0 Then
                        Using cmd As New SqlCommand("INSERT INTO mn.StockCard (PartNumber, MovementType, Quantity, ReferenceType, Remarks) VALUES (@part, @movement, @quantity, 'Adjustment', @reason)", conn, transaction)
                            cmd.Parameters.AddWithValue("@part", cmbPart.Text)
                            cmd.Parameters.AddWithValue("@movement", If(difference > 0, "IN", "OUT"))
                            cmd.Parameters.AddWithValue("@quantity", Math.Abs(difference))
                            cmd.Parameters.AddWithValue("@reason", txtReason.Text.Trim())
                            cmd.ExecuteNonQuery()
                        End Using
                    End If
                    AccountingService.WriteAudit(conn, transaction, "INVENTORY_ADJUSTED", "InventoryAdjustment", adjustmentID, txtReason.Text.Trim())
                    transaction.Commit()
                End Using
            End Using
            MessageBox.Show("Inventory adjustment saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.Close()
        Catch ex As Exception
            AppLogger.Error("Failed to save inventory adjustment", ex)
            MessageBox.Show("Unable to save adjustment: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
