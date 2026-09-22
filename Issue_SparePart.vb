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
        Me.Size = New Size(600, 600)
        Me.BackColor = Color.FromArgb(248, 249, 250)

        ' Create title
        CreateTitle()

        ' Start with customer phone check
        ShowPhoneCheckSection()
    End Sub

    ' =============== FORM TITLE ===============
    Private Sub CreateTitle()
        Dim lblTitle As New Label With {
            .Text = "ISSUE SPARE PARTS",
            .Font = New Font("Arial", 14, FontStyle.Bold),
            .ForeColor = Color.Navy,
            .Location = New Point(180, 20),
            .Size = New Size(250, 30),
            .Name = "lblTitle"
        }
        Me.Controls.Add(lblTitle)
    End Sub

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
    End Sub

    ' =============== STEP 1: PHONE CHECK ===============
    Private Sub ShowPhoneCheckSection()
        ClearFormExceptTitle()

        ' Label
        Dim lblPhone As New Label With {
            .Text = "Customer Phone Number:",
            .Location = New Point(50, 80),
            .Size = New Size(180, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblPhone)

        ' TextBox for phone
        txtPhone = New TextBox With {
            .Location = New Point(240, 80),
            .Size = New Size(200, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtPhone)

        ' Check Customer Button
        Dim btnCheck As New Button With {
            .Text = "Check Customer",
            .Location = New Point(450, 78),
            .Size = New Size(120, 30),
            .BackColor = Color.FromArgb(59, 130, 246),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 9, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCheck.Click, AddressOf btnCheckCustomer_Click
        Me.Controls.Add(btnCheck)

        ' Cancel Button
        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Location = New Point(450, 120),
            .Size = New Size(120, 30),
            .BackColor = Color.FromArgb(239, 68, 68),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 9, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        Me.Controls.Add(btnCancel)
    End Sub

    ' =============== CHECK CUSTOMER ===============
    Private Sub btnCheckCustomer_Click(sender As Object, e As EventArgs)
        Dim phoneNumber As String = txtPhone.Text.Trim()

        If String.IsNullOrEmpty(phoneNumber) Then
            MessageBox.Show("Please enter customer phone number", "Input Required",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Save phone for later use
        savedPhone = phoneNumber

        Try
            ' Use a fresh connection
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()

                ' Check if customer exists (PHP: SELECT * FROM mn.Customers WHERE PhoneNumber = ?)
                ' Note: Your database has [Phone] column, PHP uses PhoneNumber
                Dim sql As String = "SELECT CustomerID, CustomerName, Phone FROM mn.Customer WHERE Phone = @phone"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@phone", phoneNumber)

                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' Customer exists
                            customerID = reader.GetInt32(0)
                            customerName = reader.GetString(1)
                            customerExists = True

                            ' Show existing customer issue form
                            ShowExistingCustomerSection()
                        Else
                            ' New customer
                            customerExists = False

                            ' Show new customer form
                            ShowNewCustomerSection()
                        End If
                    End Using
                End Using

                conn.Close()
            End Using

        Catch ex As Exception
            MessageBox.Show("Error checking customer: " & ex.Message, "Database Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' =============== STEP 2A: NEW CUSTOMER ===============
    Private Sub ShowNewCustomerSection()
        ClearFormExceptTitle()

        ' Section Title
        Dim lblSectionTitle As New Label With {
            .Text = "New Customer Information",
            .Font = New Font("Arial", 11, FontStyle.Bold),
            .Location = New Point(50, 60),
            .Size = New Size(300, 25),
            .ForeColor = Color.DarkBlue
        }
        Me.Controls.Add(lblSectionTitle)

        ' Phone Number (READ-ONLY from step 1)
        Dim lblPhoneLabel As New Label With {
            .Text = "Phone Number:",
            .Location = New Point(50, 100),
            .Size = New Size(120, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblPhoneLabel)

        Dim txtPhoneReadOnly As New TextBox With {
            .Location = New Point(180, 100),
            .Size = New Size(200, 25),
            .Font = New Font("Arial", 10),
            .Text = savedPhone,
            .ReadOnly = True,
            .BackColor = Color.FromArgb(240, 240, 240),
            .ForeColor = Color.DarkGreen,
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(txtPhoneReadOnly)

        ' First Name (REQUIRED)
        Dim lblFirstName As New Label With {
            .Text = "First Name: *",
            .Location = New Point(50, 140),
            .Size = New Size(120, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblFirstName)

        txtFirstName = New TextBox With {
            .Location = New Point(180, 140),
            .Size = New Size(200, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtFirstName)

        ' Last Name (REQUIRED - PHP logic: both required)
        Dim lblLastName As New Label With {
            .Text = "Last Name: *",
            .Location = New Point(50, 180),
            .Size = New Size(120, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblLastName)

        txtLastName = New TextBox With {
            .Location = New Point(180, 180),
            .Size = New Size(200, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtLastName)

        ' Address (Optional)
        Dim lblAddress As New Label With {
            .Text = "Address:",
            .Location = New Point(50, 220),
            .Size = New Size(120, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblAddress)

        txtAddress = New TextBox With {
            .Location = New Point(180, 220),
            .Size = New Size(200, 60),
            .Multiline = True,
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtAddress)

        ' Email (Optional)
        Dim lblEmail As New Label With {
            .Text = "Email:",
            .Location = New Point(50, 300),
            .Size = New Size(120, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblEmail)

        txtEmail = New TextBox With {
            .Location = New Point(180, 300),
            .Size = New Size(200, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtEmail)

        ' Items Section Title
        Dim lblItemsTitle As New Label With {
            .Text = "Items to Issue:",
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .Location = New Point(50, 340),
            .Size = New Size(200, 25),
            .ForeColor = Color.DarkSlateGray
        }
        Me.Controls.Add(lblItemsTitle)

        ' Container for items (like PHP's items-container-new)
        Dim pnlItemsContainer As New Panel With {
            .Location = New Point(50, 370),
            .Size = New Size(500, 120),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Padding = New Padding(10)
        }
        Me.Controls.Add(pnlItemsContainer)

        ' Add initial item row
        AddItemRow(pnlItemsContainer)

        ' Add Item Button
        Dim btnAddItem As New Button With {
            .Text = "+ Add Item",
            .Location = New Point(50, 500),
            .Size = New Size(100, 30),
            .BackColor = Color.FromArgb(229, 231, 235),
            .Font = New Font("Arial", 9),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnAddItem.Click, Sub(s, ev) AddItemRow(pnlItemsContainer)
        Me.Controls.Add(btnAddItem)

        ' Remarks
        Dim lblRemarks As New Label With {
            .Text = "Remarks:",
            .Location = New Point(50, 540),
            .Size = New Size(120, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblRemarks)

        txtRemarks = New TextBox With {
            .Location = New Point(180, 540),
            .Size = New Size(200, 60),
            .Multiline = True,
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtRemarks)

        ' Issue Button
        Dim btnIssue As New Button With {
            .Text = "Save Customer & Issue",
            .Location = New Point(180, 620),
            .Size = New Size(180, 35),
            .BackColor = Color.FromArgb(34, 197, 94),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnIssue.Click, Sub(s, ev) ProcessIssue(True) ' True = new customer
        Me.Controls.Add(btnIssue)

        ' Cancel Button
        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Location = New Point(370, 620),
            .Size = New Size(100, 35),
            .BackColor = Color.FromArgb(239, 68, 68),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        Me.Controls.Add(btnCancel)
    End Sub

    ' =============== STEP 2B: EXISTING CUSTOMER ===============
    Private Sub ShowExistingCustomerSection()
        ClearFormExceptTitle()

        ' Customer Info
        Dim lblCustomerInfo As New Label With {
            .Text = $"Customer: {customerName}",
            .Font = New Font("Arial", 11, FontStyle.Bold),
            .Location = New Point(50, 70),
            .Size = New Size(400, 25),
            .ForeColor = Color.DarkGreen
        }
        Me.Controls.Add(lblCustomerInfo)

        ' Items Section Title
        Dim lblItemsTitle As New Label With {
            .Text = "Items to Issue:",
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .Location = New Point(50, 110),
            .Size = New Size(200, 25),
            .ForeColor = Color.DarkSlateGray
        }
        Me.Controls.Add(lblItemsTitle)

        ' Container for items (like PHP's items-container-existing)
        Dim pnlItemsContainer As New Panel With {
            .Location = New Point(50, 140),
            .Size = New Size(500, 120),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Padding = New Padding(10)
        }
        Me.Controls.Add(pnlItemsContainer)

        ' Add initial item row
        AddItemRow(pnlItemsContainer)

        ' Add Item Button
        Dim btnAddItem As New Button With {
            .Text = "+ Add Item",
            .Location = New Point(50, 270),
            .Size = New Size(100, 30),
            .BackColor = Color.FromArgb(229, 231, 235),
            .Font = New Font("Arial", 9),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnAddItem.Click, Sub(s, ev) AddItemRow(pnlItemsContainer)
        Me.Controls.Add(btnAddItem)

        ' Remarks
        Dim lblRemarks As New Label With {
            .Text = "Remarks:",
            .Location = New Point(50, 310),
            .Size = New Size(120, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblRemarks)

        txtRemarks = New TextBox With {
            .Location = New Point(180, 310),
            .Size = New Size(200, 60),
            .Multiline = True,
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtRemarks)

        ' Issue Button
        Dim btnIssue As New Button With {
            .Text = "Issue Parts",
            .Location = New Point(180, 400),
            .Size = New Size(150, 35),
            .BackColor = Color.FromArgb(59, 130, 246),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnIssue.Click, Sub(s, ev) ProcessIssue(False) ' False = existing customer
        Me.Controls.Add(btnIssue)

        ' Cancel Button
        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Location = New Point(340, 400),
            .Size = New Size(100, 35),
            .BackColor = Color.FromArgb(239, 68, 68),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        Me.Controls.Add(btnCancel)
    End Sub

    ' =============== ADD ITEM ROW (Dynamic like PHP) ===============
    Private Sub AddItemRow(container As Panel)
        Dim panel As New Panel
        panel.Size = New Size(container.Width - 20, 30)
        panel.Location = New Point(0, itemCount * 40)
        panel.BorderStyle = BorderStyle.None

        ' Part Number
        Dim txtPartNum As New TextBox With {
    .Location = New Point(0, 0),
    .Size = New Size(150, 25),
    .Font = New Font("Arial", 9)
}
        panel.Controls.Add(txtPartNum)

        ' Quantity
        Dim txtQty As New TextBox With {
    .Location = New Point(160, 0),
    .Size = New Size(80, 25),
    .Font = New Font("Arial", 9),
    .Text = "1"
}
        panel.Controls.Add(txtQty)

        ' Selling Price
        Dim txtPrice As New TextBox With {
    .Location = New Point(250, 0),
    .Size = New Size(100, 25),
    .Font = New Font("Arial", 9),
    .Text = "0.00"
}
        panel.Controls.Add(txtPrice)

        ' Remove Button
        Dim btnRemove As New Button With {
            .Text = "Remove",
            .Location = New Point(360, 0),
            .Size = New Size(80, 25),
            .BackColor = Color.FromArgb(254, 226, 226),
            .ForeColor = Color.FromArgb(153, 27, 27),
            .Font = New Font("Arial", 9),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnRemove.Click, Sub(s, ev)
                                        container.Controls.Remove(panel)
                                        itemPanels.Remove(panel)
                                        ReorderItemPanels(container)
                                    End Sub
        panel.Controls.Add(btnRemove)

        container.Controls.Add(panel)
        itemPanels.Add(panel)
        itemCount += 1
    End Sub

    Private Sub ReorderItemPanels(container As Panel)
        For i As Integer = 0 To itemPanels.Count - 1
            itemPanels(i).Location = New Point(0, i * 40)
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
                            cmd.Parameters.AddWithValue("@phone", savedPhone)
                            cmd.Parameters.AddWithValue("@email", If(String.IsNullOrEmpty(txtEmail.Text), DBNull.Value, txtEmail.Text.Trim()))
                            cmd.Parameters.AddWithValue("@address", If(String.IsNullOrEmpty(txtAddress.Text), DBNull.Value, txtAddress.Text.Trim()))

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
                        cmd.Parameters.AddWithValue("@remarks", If(String.IsNullOrEmpty(remarks), DBNull.Value, remarks))
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
                                                "FROM mn.PurchaseBatch WHERE PartNumber = @partNum"
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
                        Dim batchSql As String = "SELECT * FROM mn.PurchaseBatch " &
                                                "WHERE PartNumber = @partNum AND QuantityRemaining > 0 " &
                                                "ORDER BY PurchaseDate ASC"

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
                    Throw ex
                End Try
            End Using

        Catch ex As Exception
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