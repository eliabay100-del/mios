Imports System.Data.SqlClient

Public Class Add_SparePart

    Private txtPhoneNumber As TextBox ' For Step 1 - editable
    Private txtSupplierName As TextBox
    Private txtAddress As TextBox
    Private txtPartNumber As TextBox
    Private txtDescription As TextBox
    Private txtQuantity As TextBox
    Private txtUnitPrice As TextBox
    Private cmbCategory As ComboBox
    Private cmbCondition As ComboBox


    Private currentSupplierID As Integer = -1
    Private currentSupplierName As String = ""
    Private isNewSupplier As Boolean = False
    Private savedPhoneNumber As String = "" ' To save phone from step 1

    Private Sub Add_SparePart_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Me.Text = "Add Spare Parts"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Size = New Size(550, 550)
        Me.BackColor = Color.FromArgb(248, 249, 250)


        CreateTitle()

        ' Start with supplier check section
        ShowSupplierCheckSection()
    End Sub

    Private Sub CreateTitle()
        Dim lblTitle As New Label With {
            .Text = "ADD SPARE PARTS",
            .Font = New Font("Arial", 14, FontStyle.Bold),
            .ForeColor = Color.Navy,
            .Location = New Point(150, 20),
            .Size = New Size(250, 30),
            .Name = "lblTitle"
        }
        Me.Controls.Add(lblTitle)
    End Sub


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
    End Sub


    Private Sub ShowSupplierCheckSection()
        ClearFormExceptTitle()

        ' Label
        Dim lblPhone As New Label With {
            .Text = "Supplier Phone Number:",
            .Location = New Point(50, 80),
            .Size = New Size(180, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblPhone)

        ' TextBox for phone (EDITABLE in Step 1)
        txtPhoneNumber = New TextBox With {
            .Location = New Point(240, 80),
            .Size = New Size(150, 25),
            .Font = New Font("Arial", 10),
            .Name = "txtPhoneNumber"
        }
        Me.Controls.Add(txtPhoneNumber)

        ' Check Supplier Button
        Dim btnCheck As New Button With {
            .Text = "Check Supplier",
            .Location = New Point(400, 78),
            .Size = New Size(120, 30),
            .BackColor = Color.FromArgb(59, 130, 246),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 9, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCheck.Click, AddressOf btnCheckSupplier_Click
        Me.Controls.Add(btnCheck)

        ' Cancel Button
        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Location = New Point(400, 120),
            .Size = New Size(120, 30),
            .BackColor = Color.FromArgb(239, 68, 68),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 9, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        Me.Controls.Add(btnCancel)
    End Sub


    Private Sub btnCheckSupplier_Click(sender As Object, e As EventArgs)
        Dim phoneNumber As String = txtPhoneNumber.Text.Trim()

        If String.IsNullOrEmpty(phoneNumber) Then
            MessageBox.Show("Please enter supplier phone number", "Input Required",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        savedPhoneNumber = phoneNumber

        Try

            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()


                Dim sql As String = "SELECT SupplierID, SupplierName FROM mn.Supplier WHERE Phone = @phone"
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@phone", phoneNumber)

                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' Supplier exists
                            currentSupplierID = reader.GetInt32(0)
                            currentSupplierName = reader.GetString(1)
                            isNewSupplier = False
                        Else
                            ' New supplier
                            isNewSupplier = True
                        End If
                    End Using
                End Using

                conn.Close()
            End Using


            If Not isNewSupplier Then

                ShowPartEntrySection()
            Else

                ShowNewSupplierSection()
            End If

        Catch ex As Exception
            MessageBox.Show("Error checking supplier: " & ex.Message, "Database Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


    Private Sub ShowNewSupplierSection()
        ClearFormExceptTitle()


        Dim lblSectionTitle As New Label With {
            .Text = "New Supplier Information",
            .Font = New Font("Arial", 11, FontStyle.Bold),
            .Location = New Point(50, 60),
            .Size = New Size(300, 25),
            .ForeColor = Color.DarkBlue
        }
        Me.Controls.Add(lblSectionTitle)


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
            .Text = savedPhoneNumber,
            .ReadOnly = True, ' THIS MAKES IT READ-ONLY
            .BackColor = Color.FromArgb(240, 240, 240), ' Gray background to indicate read-only
            .ForeColor = Color.DarkGreen,
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(txtPhoneReadOnly)


        Dim lblSupName As New Label With {
            .Text = "Supplier Name: *",
            .Location = New Point(50, 140),
            .Size = New Size(120, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblSupName)

        txtSupplierName = New TextBox With {
            .Location = New Point(180, 140),
            .Size = New Size(200, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtSupplierName)


        Dim lblAddress As New Label With {
            .Text = "Address:",
            .Location = New Point(50, 180),
            .Size = New Size(120, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblAddress)

        txtAddress = New TextBox With {
            .Location = New Point(180, 180),
            .Size = New Size(200, 60),
            .Multiline = True,
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtAddress)


        Dim btnContinue As New Button With {
            .Text = "Continue to Add Parts",
            .Location = New Point(180, 260),
            .Size = New Size(180, 35),
            .BackColor = Color.FromArgb(34, 197, 94),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnContinue.Click, AddressOf btnContinueToParts_Click
        Me.Controls.Add(btnContinue)

        Dim btnBack As New Button With {
            .Text = "Back",
            .Location = New Point(370, 260),
            .Size = New Size(80, 35),
            .BackColor = Color.FromArgb(156, 163, 175),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnBack.Click, AddressOf btnBackToSupplierCheck_Click
        Me.Controls.Add(btnBack)
    End Sub

    Private Sub btnContinueToParts_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(txtSupplierName.Text.Trim()) Then
            MessageBox.Show("Supplier name is required", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtSupplierName.Focus()
            Return
        End If

        Try
            ' Use a fresh connection
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()

                ' Insert new supplier
                Dim sql As String = "INSERT INTO mn.Supplier (SupplierName, Phone, Address, CreatedAt) " &
                                   "OUTPUT INSERTED.SupplierID " &
                                   "VALUES (@name, @phone, @address, GETDATE())"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@name", txtSupplierName.Text.Trim())
                    cmd.Parameters.AddWithValue("@phone", savedPhoneNumber)
                    cmd.Parameters.AddWithValue("@address", If(String.IsNullOrEmpty(txtAddress.Text), DBNull.Value, txtAddress.Text.Trim()))

                    currentSupplierID = CInt(cmd.ExecuteScalar())
                    currentSupplierName = txtSupplierName.Text.Trim()
                End Using

                conn.Close()
            End Using

            ' Show part entry section
            ShowPartEntrySection()

        Catch ex As Exception
            MessageBox.Show("Error creating supplier: " & ex.Message, "Database Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnBackToSupplierCheck_Click(sender As Object, e As EventArgs)
        ShowSupplierCheckSection()
    End Sub


    Private Sub ShowPartEntrySection()
        ClearFormExceptTitle()


        Dim lblSupplierInfo As New Label With {
            .Text = $"Supplier: {currentSupplierName}",
            .Font = New Font("Arial", 11, FontStyle.Bold),
            .Location = New Point(50, 70),
            .Size = New Size(400, 25),
            .ForeColor = Color.DarkGreen
        }
        Me.Controls.Add(lblSupplierInfo)

        ' Part Number
        Dim lblPartNumber As New Label With {
            .Text = "Part Number:",
            .Location = New Point(50, 110),
            .Size = New Size(100, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblPartNumber)

        txtPartNumber = New TextBox With {
            .Location = New Point(160, 110),
            .Size = New Size(150, 25),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(txtPartNumber)

        ' Check Part Button
        Dim btnCheckPart As New Button With {
            .Text = "Check Part",
            .Location = New Point(320, 108),
            .Size = New Size(100, 30),
            .BackColor = Color.FromArgb(59, 130, 246),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 9, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCheckPart.Click, AddressOf btnCheckPart_Click
        Me.Controls.Add(btnCheckPart)

        ' Description Label (hidden initially)
        Dim lblDescription As New Label With {
            .Text = "Description:",
            .Location = New Point(50, 150),
            .Size = New Size(100, 25),
            .Font = New Font("Arial", 10),
            .Visible = False
        }
        Me.Controls.Add(lblDescription)


        txtDescription = New TextBox With {
            .Location = New Point(160, 150),
            .Size = New Size(260, 60),
            .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .Font = New Font("Arial", 10),
            .Visible = False
        }
        Me.Controls.Add(txtDescription)


        Dim lblQuantity As New Label With {
            .Text = "Quantity:",
            .Location = New Point(50, 230),
            .Size = New Size(100, 25),
            .Font = New Font("Arial", 10),
            .Visible = False
        }
        Me.Controls.Add(lblQuantity)


        txtQuantity = New TextBox With {
            .Location = New Point(160, 230),
            .Size = New Size(100, 25),
            .Font = New Font("Arial", 10),
            .Text = "1",
            .Visible = False
        }
        Me.Controls.Add(txtQuantity)

        Dim lblUnitPrice As New Label With {
            .Text = "Unit Price ($):",
            .Location = New Point(50, 270),
            .Size = New Size(100, 25),
            .Font = New Font("Arial", 10),
            .Visible = False
        }
        Me.Controls.Add(lblUnitPrice)


        txtUnitPrice = New TextBox With {
            .Location = New Point(160, 270),
            .Size = New Size(100, 25),
            .Font = New Font("Arial", 10),
            .Text = "0.00",
            .Visible = False
        }
        Me.Controls.Add(txtUnitPrice)


        Dim lblCategory As New Label With {
            .Text = "Category:",
            .Location = New Point(50, 310),
            .Size = New Size(100, 25),
            .Font = New Font("Arial", 10),
            .Visible = False
        }
        Me.Controls.Add(lblCategory)


        cmbCategory = New ComboBox With {
            .Location = New Point(160, 310),
            .Size = New Size(150, 25),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Arial", 10),
            .Visible = False
        }
        LoadCategories()
        Me.Controls.Add(cmbCategory)


        Dim lblCondition As New Label With {
            .Text = "Condition:",
            .Location = New Point(50, 350),
            .Size = New Size(100, 25),
            .Font = New Font("Arial", 10),
            .Visible = False
        }
        Me.Controls.Add(lblCondition)


        cmbCondition = New ComboBox With {
            .Location = New Point(160, 350),
            .Size = New Size(150, 25),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Arial", 10),
            .Visible = False
        }
        LoadConditions()
        Me.Controls.Add(cmbCondition)

        ' Submit Button (hidden initially)
        Dim btnSubmit As New Button With {
            .Text = "Add Part",
            .Location = New Point(160, 400),
            .Size = New Size(120, 35),
            .BackColor = Color.FromArgb(34, 197, 94),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Visible = False
        }
        AddHandler btnSubmit.Click, AddressOf btnSubmit_Click
        Me.Controls.Add(btnSubmit)

        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Location = New Point(290, 400),
            .Size = New Size(120, 35),
            .BackColor = Color.FromArgb(239, 68, 68),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        Me.Controls.Add(btnCancel)
    End Sub


    Private Sub LoadCategories()
        Try

            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()

                Dim sql As String = "SELECT CategoryID, CategoryName FROM mn.Category ORDER BY CategoryName"
                Using cmd As New SqlCommand(sql, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        cmbCategory.Items.Clear()
                        While reader.Read()
                            cmbCategory.Items.Add(New CategoryItem With {
                                .ID = reader.GetInt32(0),
                                .Name = reader.GetString(1)
                            })
                        End While
                    End Using
                End Using

                conn.Close()
            End Using

            If cmbCategory.Items.Count > 0 Then
                cmbCategory.SelectedIndex = 0
            End If

        Catch ex As Exception
            MessageBox.Show("Error loading categories: " & ex.Message, "Database Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


    Private Sub LoadConditions()
        Try

            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()

                Dim sql As String = "SELECT ConditionID, ConditionName FROM mn.Condition ORDER BY ConditionName"
                Using cmd As New SqlCommand(sql, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        cmbCondition.Items.Clear()
                        While reader.Read()
                            cmbCondition.Items.Add(New ConditionItem With {
                                .ID = reader.GetInt32(0),
                                .Name = reader.GetString(1)
                            })
                        End While
                    End Using
                End Using

                conn.Close()
            End Using

            If cmbCondition.Items.Count > 0 Then
                cmbCondition.SelectedIndex = 0
            End If

        Catch ex As Exception
            MessageBox.Show("Error loading conditions: " & ex.Message, "Database Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


    Private Class CategoryItem
        Public Property ID As Integer
        Public Property Name As String

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

    Private Class ConditionItem
        Public Property ID As Integer
        Public Property Name As String

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class


    Private Sub btnCheckPart_Click(sender As Object, e As EventArgs)
        Dim partNumber As String = txtPartNumber.Text.Trim()

        If String.IsNullOrEmpty(partNumber) Then
            MessageBox.Show("Please enter a part number", "Input Required",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try

            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()


                Dim sql As String = "SELECT PartNumber, Description, CategoryID FROM mn.SpareParts WHERE PartNumber = @partNumber"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@partNumber", partNumber)

                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' Part exists - pre-fill data
                            Dim description As String = ""
                            If Not reader.IsDBNull(1) Then
                                description = reader.GetString(1)
                            End If
                            Dim categoryID As Integer = If(Not reader.IsDBNull(2), reader.GetInt32(2), 1)


                            ShowPartFields()


                            txtDescription.Text = description


                            For Each item As CategoryItem In cmbCategory.Items
                                If item.ID = categoryID Then
                                    cmbCategory.SelectedItem = item
                                    Exit For
                                End If
                            Next

                            MessageBox.Show($"Part '{partNumber}' exists. Updating stock.",
                                          "Part Found", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Else

                            ShowPartFields()
                            txtDescription.Text = ""
                            cmbCategory.SelectedIndex = 0
                            cmbCondition.SelectedIndex = 0

                            MessageBox.Show($"New part '{partNumber}'. Please enter details.",
                                          "New Part", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        End If
                    End Using
                End Using

                conn.Close()
            End Using

        Catch ex As Exception
            MessageBox.Show("Error checking part: " & ex.Message, "Database Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


    Private Sub ShowPartFields()

        For Each ctrl As Control In Me.Controls
            If TypeOf ctrl Is Label AndAlso (ctrl.Text.Contains("Description") OrElse
                                           ctrl.Text.Contains("Quantity") OrElse
                                           ctrl.Text.Contains("Unit Price") OrElse
                                           ctrl.Text.Contains("Category") OrElse
                                           ctrl.Text.Contains("Condition")) Then
                ctrl.Visible = True
            End If
        Next


        txtDescription.Visible = True
        txtQuantity.Visible = True
        txtUnitPrice.Visible = True
        cmbCategory.Visible = True
        cmbCondition.Visible = True


        For Each ctrl As Control In Me.Controls
            If TypeOf ctrl Is Button AndAlso ctrl.Text = "Add Part" Then
                ctrl.Visible = True
                Exit For
            End If
        Next
    End Sub

    Private Sub btnSubmit_Click(sender As Object, e As EventArgs)

        If Not ValidatePartInputs() Then
            Return
        End If

        Dim partNumber As String = txtPartNumber.Text.Trim()
        Dim description As String = txtDescription.Text.Trim()
        Dim quantity As Integer = CInt(txtQuantity.Text)
        Dim unitPrice As Decimal = CDec(txtUnitPrice.Text)
        Dim categoryID As Integer = DirectCast(cmbCategory.SelectedItem, CategoryItem).ID
        Dim conditionID As Integer = DirectCast(cmbCondition.SelectedItem, ConditionItem).ID

        Try
            ' Use a fresh connection
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()


                Dim transaction As SqlTransaction = conn.BeginTransaction()

                Try

                    Dim checkSql As String = "SELECT PartID, Quantity FROM mn.SpareParts WHERE PartNumber = @partNumber"
                    Dim partID As Integer = 0
                    Dim existingQty As Integer = 0

                    Using cmd As New SqlCommand(checkSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@partNumber", partNumber)

                        Using reader As SqlDataReader = cmd.ExecuteReader()
                            If reader.Read() Then
                                ' Part exists - UPDATE
                                partID = reader.GetInt32(0)
                                existingQty = reader.GetInt32(1)
                            End If
                        End Using
                    End Using

                    If partID > 0 Then

                        Dim newQty As Integer = existingQty + quantity

                        Dim updateSql As String = "UPDATE mn.SpareParts SET Quantity = @qty, UnitPrice = @price, " &
                                                 "Description = @desc, CategoryID = @catID, ConditionID = @condID " &
                                                 "WHERE PartNumber = @partNumber"

                        Using cmd As New SqlCommand(updateSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@qty", newQty)
                            cmd.Parameters.AddWithValue("@price", unitPrice)
                            cmd.Parameters.AddWithValue("@desc", description)
                            cmd.Parameters.AddWithValue("@catID", categoryID)
                            cmd.Parameters.AddWithValue("@condID", conditionID)
                            cmd.Parameters.AddWithValue("@partNumber", partNumber)
                            cmd.ExecuteNonQuery()
                        End Using
                    Else
                        ' INSERT new part
                        Dim insertSql As String = "INSERT INTO mn.SpareParts " &
                                                 "(PartNumber, Description, CategoryID, ConditionID, SupplierID, " &
                                                 "Quantity, UnitPrice, CreatedAt) " &
                                                 "VALUES (@partNum, @desc, @catID, @condID, @supID, @qty, @price, GETDATE())"

                        Using cmd As New SqlCommand(insertSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@partNum", partNumber)
                            cmd.Parameters.AddWithValue("@desc", description)
                            cmd.Parameters.AddWithValue("@catID", categoryID)
                            cmd.Parameters.AddWithValue("@condID", conditionID)
                            cmd.Parameters.AddWithValue("@supID", currentSupplierID)
                            cmd.Parameters.AddWithValue("@qty", quantity)
                            cmd.Parameters.AddWithValue("@price", unitPrice)
                            cmd.ExecuteNonQuery()
                        End Using
                    End If

                    ' Insert into PurchaseBatch
                    Dim batchSql As String = "INSERT INTO mn.PurchaseBatch " &
                                            "(PartNumber, SupplierID, PurchaseDate, QuantityPurchased, QuantityRemaining, UnitCost) " &
                                            "VALUES (@partNum, @supID, GETDATE(), @qty, @qty, @cost)"

                    Using cmd As New SqlCommand(batchSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@partNum", partNumber)
                        cmd.Parameters.AddWithValue("@supID", currentSupplierID)
                        cmd.Parameters.AddWithValue("@qty", quantity)
                        cmd.Parameters.AddWithValue("@cost", unitPrice)
                        cmd.ExecuteNonQuery()
                    End Using

                    ' Insert into StockCard (audit trail)
                    Dim stockSql As String = "INSERT INTO mn.StockCard " &
                                            "(PartNumber, MovementType, Quantity, MovementDate, ReferenceType, Remarks) " &
                                            "VALUES (@partNum, 'IN', @qty, GETDATE(), 'Purchase', 'Added from spare part form')"

                    Using cmd As New SqlCommand(stockSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@partNum", partNumber)
                        cmd.Parameters.AddWithValue("@qty", quantity)
                        cmd.ExecuteNonQuery()
                    End Using


                    transaction.Commit()


                    ShowSuccess(partNumber, quantity, unitPrice)

                Catch ex As Exception

                    transaction.Rollback()
                    Throw ex
                End Try

                conn.Close()
            End Using

        Catch ex As Exception
            MessageBox.Show("Error saving part: " & ex.Message, "Database Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


    Private Function ValidatePartInputs() As Boolean

        If String.IsNullOrEmpty(txtPartNumber.Text.Trim()) Then
            MessageBox.Show("Part number is required", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtPartNumber.Focus()
            Return False
        End If


        If String.IsNullOrEmpty(txtDescription.Text.Trim()) Then
            MessageBox.Show("Description is required", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtDescription.Focus()
            Return False
        End If


        Dim qty As Integer
        If Not Integer.TryParse(txtQuantity.Text, qty) OrElse qty <= 0 Then
            MessageBox.Show("Quantity must be a positive whole number", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtQuantity.Focus()
            Return False
        End If

        Dim price As Decimal
        If Not Decimal.TryParse(txtUnitPrice.Text, price) OrElse price < 0 Then
            MessageBox.Show("Unit price must be a valid positive number", "Validation Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtUnitPrice.Focus()
            Return False
        End If

        Return True
    End Function


    Private Sub ShowSuccess(partNumber As String, quantity As Integer, unitPrice As Decimal)
        ClearFormExceptTitle()


        Dim lblSuccess As New Label With {
            .Text = $"✅ Part '{partNumber}' added successfully!",
            .Font = New Font("Arial", 12, FontStyle.Bold),
            .ForeColor = Color.DarkGreen,
            .Location = New Point(100, 100),
            .Size = New Size(350, 30)
        }
        Me.Controls.Add(lblSuccess)


        Dim lblDetails As New Label With {
            .Text = $"Quantity: {quantity}" & vbCrLf &
                   $"Unit Price: ${unitPrice:F2}" & vbCrLf &
                   $"Total: ${quantity * unitPrice:F2}" & vbCrLf &
                   $"Supplier: {currentSupplierName}",
            .Location = New Point(100, 150),
            .Size = New Size(300, 120),
            .Font = New Font("Arial", 10)
        }
        Me.Controls.Add(lblDetails)

        Dim btnAddAnother As New Button With {
            .Text = "Add Another Part",
            .Location = New Point(100, 300),
            .Size = New Size(150, 35),
            .BackColor = Color.FromArgb(59, 130, 246),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnAddAnother.Click, AddressOf btnAddAnother_Click
        Me.Controls.Add(btnAddAnother)

        ' Done button
        Dim btnDone As New Button With {
            .Text = "Done",
            .Location = New Point(270, 300),
            .Size = New Size(100, 35),
            .BackColor = Color.FromArgb(34, 197, 94),
            .ForeColor = Color.White,
            .Font = New Font("Arial", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnDone.Click, AddressOf btnDone_Click
        Me.Controls.Add(btnDone)
    End Sub

    Private Sub btnAddAnother_Click(sender As Object, e As EventArgs)
        ' Reset to part entry with same supplier
        ShowPartEntrySection()
    End Sub

    Private Sub btnDone_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub
End Class