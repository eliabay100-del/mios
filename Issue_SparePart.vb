Imports System.Data.SqlClient

Public Class Issue_SparePart
    ' Form controls
    Private txtPhone As TextBox
    Private txtFirstName As TextBox
    Private txtLastName As TextBox
    Private txtAddress As TextBox
    Private txtEmail As TextBox
    Private txtRemarks As TextBox

    ' Dynamic item controls
    Private itemPanels As New List(Of Panel)
    Private itemCount As Integer = 0

    ' State variables
    Private customerID As Integer = -1
    Private customerName As String = ""
    Private customerExists As Boolean = False
    Private savedPhone As String = ""
    Private savedSearchName As String = ""
    Private txtSearchName As TextBox
    Private txtNewCustomerPhone As TextBox

    Private Sub Issue_SparePart_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ModernTheme.ApplyWindowTheme(Me)
        If Not SecurityContext.HasPermission("INVENTORY_ISSUE") Then
            MessageBox.Show("Your role cannot issue inventory.", "Access denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.Close()
            Return
        End If
        ' Form setup
        Me.Text = "Issue Spare Parts"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Size = New Size(780, 760)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Create title
        CreateTitle()

        ' Start with customer phone check
        ShowPhoneCheckSection()
    End Sub

    ' =============== FORM TITLE ===============
    Private Sub CreateTitle()
        Dim lblTitle As New Label With {
            .Text = "Issue Spare Parts",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(30, 24),
            .Size = New Size(400, 38),
            .Name = "lblTitle"
        }
        Me.Controls.Add(lblTitle)
    End Sub

    ' ---- Small layout helpers (keeps the sections consistent) -----------
    Private Function MakeCard(x As Integer, y As Integer, w As Integer, h As Integer) As Panel
        Return New Panel With {
            .Location = New Point(x, y),
            .Size = New Size(w, h),
            .BackColor = ModernTheme.Surface
        }
    End Function

    Private Function MakeFieldLabel(text As String, x As Integer, y As Integer, Optional w As Integer = 170) As Label
        Return New Label With {
            .Text = text,
            .Location = New Point(x, y),
            .Size = New Size(w, 30),
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
    End Function

    Private Function MakeSectionTitle(text As String, x As Integer, y As Integer) As Label
        Return New Label With {
            .Text = text,
            .Location = New Point(x, y),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 15.0F, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText
        }
    End Function

    Private Function MakeHint(text As String, x As Integer, y As Integer, w As Integer) As Label
        Return New Label With {
            .Text = text,
            .Location = New Point(x, y),
            .Size = New Size(w, 24),
            .ForeColor = ModernTheme.MutedText
        }
    End Function

    Private Function MakeColumnHeading(text As String, x As Integer, y As Integer, w As Integer) As Label
        Return New Label With {
            .Text = text,
            .Location = New Point(x, y),
            .Size = New Size(w, 20),
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
    End Function

    ' The items card is shared by the new- and existing-customer screens.
    Private Function BuildItemsCard(x As Integer, y As Integer) As Panel
        Dim card As Panel = MakeCard(x, y, 680, 320)
        card.Controls.Add(MakeSectionTitle("Items to issue", 20, 16))
        card.Controls.Add(MakeColumnHeading("Part number", 30, 54, 200))
        card.Controls.Add(MakeColumnHeading("Qty", 240, 54, 90))
        card.Controls.Add(MakeColumnHeading("Unit price", 340, 54, 120))

        Dim container As New Panel With {
            .Location = New Point(20, 80),
            .Size = New Size(640, 160),
            .BackColor = ModernTheme.SurfaceAlt,
            .Padding = New Padding(10),
            .AutoScroll = True
        }
        card.Controls.Add(container)
        AddItemRow(container)

        Dim btnAddItem As Button = ModernTheme.MakeSecondaryButton("+ Add item")
        btnAddItem.Location = New Point(20, 248)
        btnAddItem.Width = 150
        btnAddItem.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnAddItem.Click, Sub(s, ev) AddItemRow(container)
        card.Controls.Add(btnAddItem)

        Return card
    End Function

    ' The notes/actions card is shared by the new- and existing-customer screens.
    Private Function BuildActionsCard(x As Integer, y As Integer, isNewCustomer As Boolean) As Panel
        Dim card As Panel = MakeCard(x, y, 680, 230)
        card.Controls.Add(MakeSectionTitle("Notes & actions", 20, 16))

        card.Controls.Add(MakeFieldLabel("Remarks", 20, 56))

        txtRemarks = ModernTheme.MakeTextBox()
        txtRemarks.Location = New Point(20, 82)
        txtRemarks.Width = 640
        txtRemarks.Height = 60
        txtRemarks.Multiline = True
        card.Controls.Add(txtRemarks)

        Dim btnIssue As Button = ModernTheme.MakePrimaryButton(If(isNewCustomer, "Save customer & issue", "Issue parts"))
        btnIssue.Location = New Point(20, 158)
        btnIssue.Width = 240
        AddHandler btnIssue.Click, Sub(s, ev) ProcessIssue(isNewCustomer)
        card.Controls.Add(btnIssue)

        Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("Cancel")
        btnCancel.Location = New Point(272, 158)
        btnCancel.Width = 140
        btnCancel.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        card.Controls.Add(btnCancel)

        Return card
    End Function

    ' =============== CLEAR FORM ===============
    Private Sub ClearFormExceptTitle()
        Dim controlsToRemove As New List(Of Control)
        For Each ctrl As Control In Me.Controls
            If ctrl.Name <> "lblTitle" Then
                controlsToRemove.Add(ctrl)
            End If
        Next

        For Each ctrl As Control In controlsToRemove
            Me.Controls.Remove(ctrl)
            ctrl.Dispose()
        Next

        ' Clear item panels
        itemPanels.Clear()
        itemCount = 0

        ' Each section is rebuilt from the top.
        Me.AutoScrollPosition = New Point(0, 0)
    End Sub

    ' =============== STEP 1: CUSTOMER CHECK ===============
    Private Sub ShowPhoneCheckSection()
        ClearFormExceptTitle()

        Dim card As Panel = MakeCard(30, 80, 680, 220)
        card.Controls.Add(MakeHint("Find a customer by name and/or phone, or click New customer.", 20, 16, 640))

        card.Controls.Add(MakeFieldLabel("Customer name", 20, 54))
        txtSearchName = ModernTheme.MakeTextBox()
        txtSearchName.Location = New Point(190, 50)
        txtSearchName.Width = 240
        card.Controls.Add(txtSearchName)

        card.Controls.Add(MakeFieldLabel("Phone number", 20, 102))
        txtPhone = ModernTheme.MakeTextBox()
        txtPhone.Location = New Point(190, 98)
        txtPhone.Width = 240
        card.Controls.Add(txtPhone)

        Dim btnCheck As Button = ModernTheme.MakePrimaryButton("Check customer")
        btnCheck.Location = New Point(450, 50)
        btnCheck.Width = 200
        AddHandler btnCheck.Click, AddressOf btnCheckCustomer_Click
        card.Controls.Add(btnCheck)

        Dim btnNew As Button = ModernTheme.MakeSecondaryButton("New customer")
        btnNew.Location = New Point(450, 98)
        btnNew.Width = 200
        btnNew.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnNew.Click, AddressOf btnNewCustomer_Click
        card.Controls.Add(btnNew)

        Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("Cancel")
        btnCancel.Location = New Point(20, 152)
        btnCancel.Width = 140
        btnCancel.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        card.Controls.Add(btnCancel)

        Me.Controls.Add(card)
        ModernTheme.RefreshContent(Me)
        FitToContent()
    End Sub

    ''' <summary>
    ''' Resizes the dialog so the current section is framed snugly, without ever
    ''' growing past the working area of the screen it is shown on (the taller
    ''' sections then simply scroll).
    ''' </summary>
    Private Sub FitToContent()
        Me.PerformLayout()

        Dim contentBottom As Integer = 0
        For Each ctrl As Control In Me.Controls
            If ctrl.Visible Then contentBottom = Math.Max(contentBottom, ctrl.Bottom)
        Next
        If contentBottom <= 0 Then Return

        Dim frame As Integer = Me.Height - Me.ClientSize.Height
        Dim target As Integer = contentBottom + 24 + frame
        Dim maxHeight As Integer = Screen.FromControl(Me).WorkingArea.Height - 24
        If target > maxHeight Then target = maxHeight
        If target < 240 Then target = 240

        If target <> Me.Height Then Me.Height = target

        ' Rebuilding a section leaves the previous section's scroll offset behind,
        ' which would cut the top card off. Pin the view back to the title.
        If AutoScrollPosition.Y <> 0 Then AutoScrollPosition = New Point(0, 0)
    End Sub

    ' =============== CHECK CUSTOMER ===============
    Private Sub btnCheckCustomer_Click(sender As Object, e As EventArgs)
        Dim customerNameInput As String = txtSearchName.Text.Trim()
        Dim phoneNumber As String = txtPhone.Text.Trim()

        If String.IsNullOrEmpty(customerNameInput) AndAlso String.IsNullOrEmpty(phoneNumber) Then
            MessageBox.Show("Enter a customer name and/or phone number to check, or click New Customer.",
                          "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtSearchName.Focus()
            Return
        End If

        ' Save what was typed so a new-customer form can reuse it
        savedSearchName = customerNameInput
        savedPhone = phoneNumber

        Try
            Dim matches As New List(Of CustomerMatch)
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()

                Dim clauses As New List(Of String)
                If phoneNumber.Length > 0 Then clauses.Add("Phone = @phone")
                If customerNameInput.Length > 0 Then clauses.Add("CustomerName LIKE @name")
                Dim sql As String = "SELECT CustomerID, CustomerName, Phone FROM mn.Customer WHERE " &
                                    String.Join(" OR ", clauses) & " ORDER BY CustomerName"
                Using cmd As New SqlCommand(sql, conn)
                    If phoneNumber.Length > 0 Then cmd.Parameters.AddWithValue("@phone", phoneNumber)
                    If customerNameInput.Length > 0 Then cmd.Parameters.AddWithValue("@name", "%" & customerNameInput & "%")
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            matches.Add(New CustomerMatch With {
                                .ID = reader.GetInt32(0),
                                .Name = reader.GetString(1),
                                .Phone = If(reader.IsDBNull(2), "", reader.GetString(2))
                            })
                        End While
                    End Using
                End Using

                conn.Close()
            End Using

            If matches.Count = 0 Then
                customerExists = False
                ShowNewCustomerSection()
            ElseIf matches.Count = 1 Then
                customerID = matches(0).ID
                customerName = matches(0).Name
                customerExists = True
                ShowExistingCustomerSection()
            Else
                ShowCustomerPickerSection(matches)
            End If

        Catch ex As Exception
            AppLogger.Error("Failed to check customer", ex)
            MessageBox.Show("Error checking customer: " & ex.Message, "Database Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' =============== NEW CUSTOMER (from the check screen) ===============
    Private Sub btnNewCustomer_Click(sender As Object, e As EventArgs)
        savedSearchName = txtSearchName.Text.Trim()
        savedPhone = txtPhone.Text.Trim()
        customerExists = False
        ShowNewCustomerSection()
    End Sub

    ' =============== PICK BETWEEN SEVERAL MATCHES ===============
    Private Sub ShowCustomerPickerSection(matches As List(Of CustomerMatch))
        ClearFormExceptTitle()

        Dim card As Panel = MakeCard(30, 80, 680, 330)
        card.Controls.Add(MakeSectionTitle("Several customers match - choose one", 20, 18))

        Dim list As New ListBox With {
            .Location = New Point(20, 62),
            .Size = New Size(640, 170),
            .Font = New Font("Segoe UI", 10.0F),
            .BorderStyle = BorderStyle.FixedSingle,
            .IntegralHeight = False
        }
        For Each match As CustomerMatch In matches
            list.Items.Add(match)
        Next
        If list.Items.Count > 0 Then list.SelectedIndex = 0
        card.Controls.Add(list)

        Dim btnUse As Button = ModernTheme.MakePrimaryButton("Use selected customer")
        btnUse.Location = New Point(20, 250)
        btnUse.Width = 220
        AddHandler btnUse.Click, Sub(s, ev)
                                     Dim chosen As CustomerMatch = TryCast(list.SelectedItem, CustomerMatch)
                                     If chosen Is Nothing Then Return
                                     customerID = chosen.ID
                                     customerName = chosen.Name
                                     customerExists = True
                                     ShowExistingCustomerSection()
                                 End Sub
        card.Controls.Add(btnUse)

        Dim btnNew As Button = ModernTheme.MakeSecondaryButton("Create new customer")
        btnNew.Location = New Point(252, 250)
        btnNew.Width = 200
        btnNew.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnNew.Click, AddressOf btnNewCustomer_Click
        card.Controls.Add(btnNew)

        Dim btnBack As Button = ModernTheme.MakeSecondaryButton("Back")
        btnBack.Location = New Point(464, 250)
        btnBack.Width = 196
        btnBack.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnBack.Click, Sub(s, ev) ShowPhoneCheckSection()
        card.Controls.Add(btnBack)

        Me.Controls.Add(card)
        ModernTheme.RefreshContent(Me)
        FitToContent()
    End Sub

    Private Class CustomerMatch
        Public Property ID As Integer
        Public Property Name As String
        Public Property Phone As String
        Public Overrides Function ToString() As String
            Return If(String.IsNullOrEmpty(Phone), Name, Name & "  (" & Phone & ")")
        End Function
    End Class

    ' =============== STEP 2A: NEW CUSTOMER ===============
    Private Sub ShowNewCustomerSection()
        ClearFormExceptTitle()

        Dim infoCard As Panel = MakeCard(30, 80, 680, 220)
        infoCard.Controls.Add(MakeSectionTitle("New customer", 20, 16))

        infoCard.Controls.Add(MakeFieldLabel("First name *", 20, 56, 130))
        txtFirstName = ModernTheme.MakeTextBox()
        txtFirstName.Location = New Point(155, 52)
        txtFirstName.Width = 180
        infoCard.Controls.Add(txtFirstName)

        infoCard.Controls.Add(MakeFieldLabel("Last name *", 360, 56, 130))
        txtLastName = ModernTheme.MakeTextBox()
        txtLastName.Location = New Point(495, 52)
        txtLastName.Width = 165
        infoCard.Controls.Add(txtLastName)

        infoCard.Controls.Add(MakeFieldLabel("Phone", 20, 98, 130))
        txtNewCustomerPhone = ModernTheme.MakeTextBox()
        txtNewCustomerPhone.Location = New Point(155, 94)
        txtNewCustomerPhone.Width = 180
        txtNewCustomerPhone.Text = savedPhone
        infoCard.Controls.Add(txtNewCustomerPhone)

        infoCard.Controls.Add(MakeFieldLabel("Email", 360, 98, 130))
        txtEmail = ModernTheme.MakeTextBox()
        txtEmail.Location = New Point(495, 94)
        txtEmail.Width = 165
        infoCard.Controls.Add(txtEmail)

        infoCard.Controls.Add(MakeFieldLabel("Address", 20, 140, 130))
        txtAddress = ModernTheme.MakeTextBox()
        txtAddress.Location = New Point(155, 136)
        txtAddress.Width = 505
        txtAddress.Height = 56
        txtAddress.Multiline = True
        infoCard.Controls.Add(txtAddress)

        Me.Controls.Add(infoCard)
        Me.Controls.Add(BuildItemsCard(30, 320))
        Me.Controls.Add(BuildActionsCard(30, 660, True))
        ModernTheme.RefreshContent(Me)
        FitToContent()
    End Sub

    ' =============== STEP 2B: EXISTING CUSTOMER ===============
    Private Sub ShowExistingCustomerSection()
        ClearFormExceptTitle()

        Dim customerCard As Panel = MakeCard(30, 80, 680, 92)
        customerCard.Controls.Add(MakeFieldLabel("Customer", 20, 16))
        customerCard.Controls.Add(New Label With {
            .Text = customerName,
            .Location = New Point(20, 42),
            .Size = New Size(460, 32),
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .ForeColor = ModernTheme.Accent
        })
        Dim btnChange As Button = ModernTheme.MakeSecondaryButton("Change customer")
        btnChange.Location = New Point(500, 24)
        btnChange.Width = 160
        btnChange.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnChange.Click, Sub(s, ev) ShowPhoneCheckSection()
        customerCard.Controls.Add(btnChange)
        Me.Controls.Add(customerCard)

        Me.Controls.Add(BuildItemsCard(30, 192))
        Me.Controls.Add(BuildActionsCard(30, 532, False))
        ModernTheme.RefreshContent(Me)
        FitToContent()
    End Sub

    ' =============== ADD ITEM ROW (Dynamic) ===============
    Private Sub AddItemRow(container As Panel)
        Dim panel As New Panel With {
            .Size = New Size(container.Width - 30, 36),
            .Location = New Point(0, itemCount * 44),
            .BackColor = ModernTheme.SurfaceAlt
        }

        Dim txtPartNum As TextBox = ModernTheme.MakeTextBox()
        txtPartNum.Location = New Point(0, 0)
        txtPartNum.Width = 200
        txtPartNum.Font = New Font("Segoe UI", 9.5F)
        panel.Controls.Add(txtPartNum)

        Dim txtQty As TextBox = ModernTheme.MakeTextBox()
        txtQty.Location = New Point(210, 0)
        txtQty.Width = 90
        txtQty.Font = New Font("Segoe UI", 9.5F)
        txtQty.Text = "1"
        panel.Controls.Add(txtQty)

        Dim txtPrice As TextBox = ModernTheme.MakeTextBox()
        txtPrice.Location = New Point(310, 0)
        txtPrice.Width = 120
        txtPrice.Font = New Font("Segoe UI", 9.5F)
        txtPrice.Text = "0.00"
        panel.Controls.Add(txtPrice)

        Dim btnRemove As Button = ModernTheme.MakeSecondaryButton("Remove")
        btnRemove.Location = New Point(440, 0)
        btnRemove.Width = 100
        btnRemove.Height = 34
        btnRemove.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        btnRemove.ForeColor = ModernTheme.Danger
        btnRemove.BackColor = ModernTheme.Surface
        AddHandler btnRemove.Click, Sub(s, ev)
                                        container.Controls.Remove(panel)
                                        itemPanels.Remove(panel)
                                        panel.Dispose()
                                        ReorderItemPanels(container)
                                    End Sub
        panel.Controls.Add(btnRemove)

        container.Controls.Add(panel)
        itemPanels.Add(panel)
        itemCount += 1
        ModernTheme.RefreshContent(panel)
    End Sub

    Private Sub ReorderItemPanels(container As Panel)
        For i As Integer = 0 To itemPanels.Count - 1
            itemPanels(i).Location = New Point(0, i * 44)
        Next
        itemCount = itemPanels.Count
    End Sub

    ' =============== PROCESS ISSUE (MAIN INVENTORY LOGIC) ===============
    Private Sub ProcessIssue(isNewCustomer As Boolean)
        ' Validate customer info for new customer
        If isNewCustomer Then
            If String.IsNullOrEmpty(txtFirstName.Text.Trim()) OrElse
               String.IsNullOrEmpty(txtLastName.Text.Trim()) Then
                MessageBox.Show("First Name and Last Name are required", "Validation Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
        End If

        ' Collect items (like PHP's collect_items_from_post)
        Dim items As New List(Of ItemData)
        For Each panel As Panel In itemPanels
            Dim partNum As String = panel.Controls(0).Text.Trim()
            Dim qtyText As String = panel.Controls(1).Text.Trim()
            Dim priceText As String = panel.Controls(2).Text.Trim()

            ' Skip empty rows (like PHP logic)
            If String.IsNullOrEmpty(partNum) AndAlso
               (String.IsNullOrEmpty(qtyText) OrElse qtyText = "0") AndAlso
               (String.IsNullOrEmpty(priceText) OrElse priceText = "0.00") Then
                Continue For
            End If

            ' Parse values
            Dim quantity As Integer
            Dim price As Decimal

            If Not Integer.TryParse(qtyText, quantity) OrElse quantity <= 0 Then
                MessageBox.Show($"Invalid quantity for part '{partNum}'", "Validation Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            If Not Decimal.TryParse(priceText, price) OrElse price <= 0 Then
                MessageBox.Show($"Invalid price for part '{partNum}'", "Validation Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            items.Add(New ItemData With {
                .PartNumber = partNum,
                .Quantity = quantity,
                .SellingPrice = price
            })
        Next

        ' Validate at least one item (like PHP's validate_items)
        If items.Count = 0 Then
            MessageBox.Show("Please add at least one valid item", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' Get remarks
        Dim remarks As String = txtRemarks.Text.Trim()

        ' Start transaction (PHP: sqlsrv_begin_transaction)
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Dim transaction As SqlTransaction = conn.BeginTransaction()

                Try
                    ' Handle customer (new or existing)
                    Dim issuedTo As String = ""
                    Dim invoiceID As Integer
                    Dim totalRevenue As Decimal = 0
                    Dim totalCOGSForInvoice As Decimal = 0

                    If isNewCustomer Then
                        ' Insert new customer (PHP: INSERT INTO mn.Customers)
                        Dim insertSql As String = "INSERT INTO mn.Customer " &
                                                 "(CustomerName, Phone, Email, Address, CreatedAt) " &
                                                 "OUTPUT INSERTED.CustomerID " &
                                                 "VALUES (@name, @phone, @email, @address, GETDATE())"

                        Using cmd As New SqlCommand(insertSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@name", $"{txtFirstName.Text.Trim()} {txtLastName.Text.Trim()}")
                            Dim newPhone As String = If(txtNewCustomerPhone IsNot Nothing, txtNewCustomerPhone.Text.Trim(), savedPhone)
                            cmd.Parameters.AddWithValue("@phone", If(String.IsNullOrEmpty(newPhone), DBNull.Value, CObj(newPhone)))
                            cmd.Parameters.AddWithValue("@email", If(String.IsNullOrEmpty(txtEmail.Text), DBNull.Value, CObj(txtEmail.Text.Trim())))
                            cmd.Parameters.AddWithValue("@address", If(String.IsNullOrEmpty(txtAddress.Text), DBNull.Value, CObj(txtAddress.Text.Trim())))

                            customerID = CInt(cmd.ExecuteScalar())
                            issuedTo = $"{txtFirstName.Text.Trim()} {txtLastName.Text.Trim()}"
                        End Using
                    Else
                        ' Get existing customer name
                        issuedTo = customerName
                    End If

                    Dim invoiceSql As String = "INSERT INTO mn.SalesInvoice (CustomerID, Subtotal, TaxAmount, TotalAmount, AmountPaid, Status, Remarks) OUTPUT INSERTED.InvoiceID VALUES (@customerID, 0, 0, 0, 0, 'UNPAID', @remarks)"
                    Using cmd As New SqlCommand(invoiceSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@customerID", customerID)
                        cmd.Parameters.AddWithValue("@remarks", If(String.IsNullOrEmpty(remarks), DBNull.Value, CObj(remarks)))
                        invoiceID = Convert.ToInt32(cmd.ExecuteScalar())
                    End Using

                    ' Process each item
                    For Each item As ItemData In items
                        ' VALIDATION 1: Check if part exists (PHP: SELECT COUNT(*) FROM mn.SpareParts)
                        Dim checkSql As String = "SELECT COUNT(*) AS count FROM mn.SpareParts WHERE PartNumber = @partNum"
                        Using cmd As New SqlCommand(checkSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@partNum", item.PartNumber)
                            Using reader As SqlDataReader = cmd.ExecuteReader()
                                If reader.Read() Then
                                    If CInt(reader("count")) = 0 Then
                                        Throw New Exception($"There is no such kind of part: {item.PartNumber}")
                                    End If
                                End If
                            End Using
                        End Using

                        ' VALIDATION 2: Check stock availability (PHP: SELECT SUM(QuantityRemaining))
                        Dim stockSql As String = "SELECT SUM(QuantityRemaining) AS TotalAvailable " &
                                                "FROM mn.PurchaseBatch WITH (UPDLOCK, ROWLOCK) WHERE PartNumber = @partNum"
                        Dim availableQty As Integer = 0

                        Using cmd As New SqlCommand(stockSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@partNum", item.PartNumber)
                            Using reader As SqlDataReader = cmd.ExecuteReader()
                                If reader.Read() Then
                                    availableQty = If(reader.IsDBNull(0), 0, CInt(reader("TotalAvailable")))
                                End If
                            End Using
                        End Using

                        ' Stock validations (exact PHP logic)
                        If availableQty <= 0 Then
                            Throw New Exception($"We are out of part: {item.PartNumber}")
                        ElseIf item.Quantity > availableQty Then
                            Throw New Exception($"There is not that much quantity for part: {item.PartNumber}")
                        End If

                        ' FIFO STOCK DEDUCTION (PHP: ORDER BY DatePurchased ASC)
                        Dim qtyToIssue As Integer = item.Quantity
                        Dim originalQty As Integer = item.Quantity ' Save for later use

                        ' Get batches with available stock, oldest first
                        Dim batchSql As String = "SELECT * FROM mn.PurchaseBatch WITH (UPDLOCK, ROWLOCK) " &
                                                "WHERE PartNumber = @partNum AND QuantityRemaining > 0 " &
                                                "ORDER BY PurchaseDate ASC, BatchID ASC"

                        Dim batches As New List(Of BatchData)
                        Dim totalCOGS As Decimal = 0

                        Using cmd As New SqlCommand(batchSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@partNum", item.PartNumber)

                            Using reader As SqlDataReader = cmd.ExecuteReader()
                                While qtyToIssue > 0 AndAlso reader.Read()
                                    Dim batchID As Integer = reader.GetInt32(reader.GetOrdinal("BatchID"))
                                    Dim batchQtyRemaining As Integer = reader.GetInt32(reader.GetOrdinal("QuantityRemaining"))
                                    Dim unitCost As Decimal = reader.GetDecimal(reader.GetOrdinal("UnitCost"))

                                    Dim batchQtyUsed As Integer = Math.Min(qtyToIssue, batchQtyRemaining)

                                    batches.Add(New BatchData With {
                                        .BatchID = batchID,
                                        .PartNumber = item.PartNumber,
                                        .QuantityIssued = batchQtyUsed,
                                        .UnitCost = unitCost,
                                        .SellingPrice = item.SellingPrice
                                    })

                                    totalCOGS += batchQtyUsed * unitCost
                                    qtyToIssue -= batchQtyUsed
                                End While
                            End Using
                        End Using

                        ' Check if we couldn't fulfill the order
                        If qtyToIssue > 0 Then
                            Throw New Exception($"Not enough stock while issuing {item.PartNumber}")
                        End If

                        totalRevenue += originalQty * item.SellingPrice
                        totalCOGSForInvoice += totalCOGS
                        Dim invoiceLineSql As String = "INSERT INTO mn.SalesInvoiceLine (InvoiceID, PartNumber, Quantity, UnitPrice, UnitCost) VALUES (@invoiceID, @partNumber, @quantity, @unitPrice, @unitCost)"
                        Using cmd As New SqlCommand(invoiceLineSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@invoiceID", invoiceID)
                            cmd.Parameters.AddWithValue("@partNumber", item.PartNumber)
                            cmd.Parameters.AddWithValue("@quantity", originalQty)
                            cmd.Parameters.AddWithValue("@unitPrice", item.SellingPrice)
                            cmd.Parameters.AddWithValue("@unitCost", totalCOGS / originalQty)
                            cmd.ExecuteNonQuery()
                        End Using

                        ' PROCESS EACH BATCH
                        For Each batch As BatchData In batches
                            ' Insert into IssuedItem (PHP: INSERT INTO mn.IssuedParts)
                            ' Note: Your database has mn.IssuedItem, PHP uses mn.IssuedParts
                            Dim issuedItemSql As String = "INSERT INTO mn.IssuedItem " &
                                                         "(PartNumber, CustomerID, QuantityIssued, IssueDate) " &
                                                         "VALUES (@partNum, @custID, @qty, GETDATE())"

                            Using cmd As New SqlCommand(issuedItemSql, conn, transaction)
                                cmd.Parameters.AddWithValue("@partNum", batch.PartNumber)
                                cmd.Parameters.AddWithValue("@custID", customerID)
                                cmd.Parameters.AddWithValue("@qty", batch.QuantityIssued)
                                cmd.ExecuteNonQuery()
                            End Using

                            ' Update batch remaining quantity (PHP: UPDATE mn.PurchaseBatches)
                            Dim updateBatchSql As String = "UPDATE mn.PurchaseBatch " &
                                                          "SET QuantityRemaining = QuantityRemaining - @qtyUsed " &
                                                          "WHERE BatchID = @batchID"

                            Using cmd As New SqlCommand(updateBatchSql, conn, transaction)
                                cmd.Parameters.AddWithValue("@qtyUsed", batch.QuantityIssued)
                                cmd.Parameters.AddWithValue("@batchID", batch.BatchID)
                                cmd.ExecuteNonQuery()
                            End Using
                        Next

                        ' Update main SpareParts quantity (PHP: UPDATE mn.SpareParts)
                        Dim updateMainSql As String = "UPDATE mn.SpareParts " &
                                                     "SET Quantity = Quantity - @qty " &
                                                     "WHERE PartNumber = @partNum"

                        Using cmd As New SqlCommand(updateMainSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@qty", originalQty)
                            cmd.Parameters.AddWithValue("@partNum", item.PartNumber)
                            cmd.ExecuteNonQuery()
                        End Using

                        ' Get new balance for audit
                        Dim balanceSql As String = "SELECT Quantity FROM mn.SpareParts WHERE PartNumber = @partNum"
                        Dim balance As Integer = 0

                        Using cmd As New SqlCommand(balanceSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@partNum", item.PartNumber)
                            Using reader As SqlDataReader = cmd.ExecuteReader()
                                If reader.Read() Then
                                    balance = reader.GetInt32(0)
                                End If
                            End Using
                        End Using

                        ' Insert into StockCard for audit trail (PHP: INSERT INTO mn.StockCard)
                        Dim stockCardSql As String = "INSERT INTO mn.StockCard " &
                                                    "(PartNumber, MovementType, Quantity, MovementDate, ReferenceType, Remarks) " &
                                                    "VALUES (@partNum, 'OUT', @qty, GETDATE(), 'Issue', @remarks)"

                        Using cmd As New SqlCommand(stockCardSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@partNum", item.PartNumber)
                            cmd.Parameters.AddWithValue("@qty", originalQty)
                            cmd.Parameters.AddWithValue("@remarks", If(String.IsNullOrEmpty(remarks), "Issued to customer", remarks))
                            cmd.ExecuteNonQuery()
                        End Using
                    Next

                    Dim updateInvoiceSql As String = "UPDATE mn.SalesInvoice SET Subtotal = @subtotal, TotalAmount = @total WHERE InvoiceID = @invoiceID"
                    Using cmd As New SqlCommand(updateInvoiceSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@subtotal", totalRevenue)
                        cmd.Parameters.AddWithValue("@total", totalRevenue)
                        cmd.Parameters.AddWithValue("@invoiceID", invoiceID)
                        cmd.ExecuteNonQuery()
                    End Using

                    AccountingService.PostJournalEntry(conn, transaction, "SALE", invoiceID, "Spare parts issued to customer", New List(Of JournalLineData) From {
                        New JournalLineData With {.AccountCode = "1100", .Debit = totalRevenue, .Credit = 0},
                        New JournalLineData With {.AccountCode = "4000", .Debit = 0, .Credit = totalRevenue},
                        New JournalLineData With {.AccountCode = "5000", .Debit = totalCOGSForInvoice, .Credit = 0},
                        New JournalLineData With {.AccountCode = "1200", .Debit = 0, .Credit = totalCOGSForInvoice}
                    })

                    ' Commit transaction (PHP: sqlsrv_commit)
                    transaction.Commit()

                    ' Show success
                    MessageBox.Show("Spare parts issued successfully!", "Success",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information)

                    ' Close form
                    Me.Close()

                Catch ex As Exception
                    ' Rollback transaction (PHP: sqlsrv_rollback)
                    transaction.Rollback()
                    Throw
                End Try
            End Using

        Catch ex As Exception
            AppLogger.Error("Failed to issue spare parts", ex)
            MessageBox.Show("Error issuing parts: " & ex.Message, "Database Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' =============== DATA STRUCTURES ===============
    Private Class ItemData
        Public Property PartNumber As String
        Public Property Quantity As Integer
        Public Property SellingPrice As Decimal
    End Class

    Private Class BatchData
        Public Property BatchID As Integer
        Public Property PartNumber As String
        Public Property QuantityIssued As Integer
        Public Property UnitCost As Decimal
        Public Property SellingPrice As Decimal
    End Class

    ' =============== CANCEL BUTTON ===============
    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub
End Class