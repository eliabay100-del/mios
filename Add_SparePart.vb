Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class Add_SparePart

    Private txtPhoneNumber As TextBox
    Private txtSupplierName As TextBox
    Private txtAddress As TextBox
    Private txtPartNumber As ComboBox
    Private txtDescription As TextBox
    Private txtQuantity As TextBox
    Private txtUnitPrice As TextBox
    Private cmbCategory As ComboBox
    Private cmbCondition As ComboBox

    Private currentSupplierID As Integer = -1
    Private currentSupplierName As String = ""
    Private isNewSupplier As Boolean = False
    Private savedPhoneNumber As String = ""
    Private savedSupplierName As String = ""
    Private txtSearchName As TextBox
    Private txtNewSupplierPhone As TextBox
    Private cmbPaymentType As ComboBox
    Private allPartNumbers As List(Of String) = Nothing

    ' A brand-new supplier is not written to the database until the part it
    ' supplies is saved, so backing out of the form cannot leave a supplier with
    ' no stock behind.
    Private pendingSupplierPhone As String = ""
    Private pendingSupplierAddress As String = ""
    Private suppressCloseConfirmation As Boolean = False

    Private Sub Add_SparePart_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Not SecurityContext.HasPermission("INVENTORY_RECEIVE") Then
            MessageBox.Show("Your role cannot receive inventory.", "Access denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.Close()
            Return
        End If
        ModernTheme.ApplyWindowTheme(Me)
        Me.Text = "Add Spare Parts"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.WindowState = FormWindowState.Maximized
        Me.AutoScroll = True
        CreateTitle()
        ShowSupplierCheckSection()
    End Sub

    Private Sub CreateTitle()
        Dim lblTitle As New Label With {
            .Text = "Add Spare Parts",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(30, 24),
            .Size = New Size(300, 38),
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

    ''' <summary>
    ''' Resizes the dialog so the current section is framed snugly, without ever
    ''' growing past the working area of the screen it is shown on (the taller
    ''' sections then simply scroll). Keeps the dialog consistent with the issue
    ''' form instead of leaving a large empty area under short sections.
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

    Private Sub ShowSupplierCheckSection()
        ClearFormExceptTitle()

        Dim panel As New Panel With {
            .Location = New Point(30, 80),
            .Size = New Size(640, 200),
            .BackColor = Color.White,
            .Padding = New Padding(20)
        }
        Me.Controls.Add(panel)

        Dim lblHint As New Label With {
            .Text = "Find an existing supplier by name and/or phone, or click New supplier.",
            .Location = New Point(20, 16),
            .Size = New Size(590, 24),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblHint)

        Dim lblName As New Label With {
            .Text = "Supplier name",
            .Location = New Point(20, 54),
            .Size = New Size(160, 30),
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblName)

        txtSearchName = ModernTheme.MakeTextBox()
        txtSearchName.Location = New Point(190, 50)
        txtSearchName.Width = 220
        panel.Controls.Add(txtSearchName)

        Dim lblPhone As New Label With {
            .Text = "Phone number",
            .Location = New Point(20, 102),
            .Size = New Size(160, 30),
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblPhone)

        txtPhoneNumber = ModernTheme.MakeTextBox()
        txtPhoneNumber.Location = New Point(190, 98)
        txtPhoneNumber.Width = 220
        panel.Controls.Add(txtPhoneNumber)

        Dim btnCheck As Button = ModernTheme.MakePrimaryButton("Check supplier")
        btnCheck.Location = New Point(430, 50)
        btnCheck.Width = 170
        AddHandler btnCheck.Click, AddressOf btnCheckSupplier_Click
        panel.Controls.Add(btnCheck)

        Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("New supplier")
        btnCancel.Location = New Point(430, 102)
        btnCancel.Width = 170
        btnCancel.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnCancel.Click, AddressOf btnNewSupplier_Click
        panel.Controls.Add(btnCancel)
        ModernTheme.RefreshContent(Me)
        FitToContent()
    End Sub

    Private Sub btnCheckSupplier_Click(sender As Object, e As EventArgs)
        Dim supplierName As String = txtSearchName.Text.Trim()
        Dim phoneNumber As String = txtPhoneNumber.Text.Trim()

        If String.IsNullOrEmpty(supplierName) AndAlso String.IsNullOrEmpty(phoneNumber) Then
            MessageBox.Show("Enter a supplier name and/or phone number to check.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtSearchName.Focus()
            Return
        End If

        savedSupplierName = supplierName
        savedPhoneNumber = phoneNumber

        Try
            Dim matches As New List(Of SupplierMatch)
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Dim clauses As New List(Of String)
                If phoneNumber.Length > 0 Then clauses.Add("Phone = @phone")
                If supplierName.Length > 0 Then clauses.Add("SupplierName LIKE @name")
                Dim sql As String = "SELECT SupplierID, SupplierName, Phone FROM mn.Supplier WHERE " &
                                    String.Join(" OR ", clauses) & " ORDER BY SupplierName"
                Using cmd As New SqlCommand(sql, conn)
                    If phoneNumber.Length > 0 Then cmd.Parameters.AddWithValue("@phone", phoneNumber)
                    If supplierName.Length > 0 Then cmd.Parameters.AddWithValue("@name", "%" & supplierName & "%")
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            matches.Add(New SupplierMatch With {
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
                isNewSupplier = True
                ShowNewSupplierSection()
            ElseIf matches.Count = 1 Then
                currentSupplierID = matches(0).ID
                currentSupplierName = matches(0).Name
                isNewSupplier = False
                ShowPartEntrySection()
            Else
                ShowSupplierPickerSection(matches)
            End If

        Catch ex As Exception
            AppLogger.Error("Failed to check supplier", ex)
            MessageBox.Show("Error checking supplier: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnNewSupplier_Click(sender As Object, e As EventArgs)
        savedSupplierName = txtSearchName.Text.Trim()
        savedPhoneNumber = txtPhoneNumber.Text.Trim()
        isNewSupplier = True
        ShowNewSupplierSection()
    End Sub

    Private Sub ShowSupplierPickerSection(matches As List(Of SupplierMatch))
        ClearFormExceptTitle()

        Dim panel As New Panel With {
            .Location = New Point(30, 80),
            .Size = New Size(640, 320),
            .BackColor = Color.White,
            .Padding = New Padding(20)
        }
        Me.Controls.Add(panel)

        Dim lblSectionTitle As New Label With {
            .Text = "Several suppliers match - choose one",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(20, 16),
            .AutoSize = True
        }
        panel.Controls.Add(lblSectionTitle)

        Dim list As New ListBox With {
            .Location = New Point(20, 60),
            .Size = New Size(580, 150),
            .Font = New Font("Segoe UI", 10.0F),
            .IntegralHeight = False
        }
        For Each match As SupplierMatch In matches
            list.Items.Add(match)
        Next
        If list.Items.Count > 0 Then list.SelectedIndex = 0
        panel.Controls.Add(list)

        Dim btnUse As Button = ModernTheme.MakePrimaryButton("Use selected supplier")
        btnUse.Location = New Point(20, 230)
        btnUse.Width = 220
        AddHandler btnUse.Click, Sub(s, ev)
                                     Dim chosen As SupplierMatch = TryCast(list.SelectedItem, SupplierMatch)
                                     If chosen Is Nothing Then Return
                                     currentSupplierID = chosen.ID
                                     currentSupplierName = chosen.Name
                                     isNewSupplier = False
                                     ShowPartEntrySection()
                                 End Sub
        panel.Controls.Add(btnUse)

        Dim btnNew As Button = ModernTheme.MakeSecondaryButton("Create new supplier")
        btnNew.Location = New Point(260, 230)
        btnNew.Width = 190
        btnNew.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnNew.Click, Sub(s, ev)
                                     isNewSupplier = True
                                     ShowNewSupplierSection()
                                 End Sub
        panel.Controls.Add(btnNew)

        Dim btnBack As Button = ModernTheme.MakeSecondaryButton("Back")
        btnBack.Location = New Point(470, 230)
        btnBack.Width = 130
        btnBack.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnBack.Click, AddressOf btnBackToSupplierCheck_Click
        panel.Controls.Add(btnBack)
        ModernTheme.RefreshContent(Me)
        FitToContent()
    End Sub

    Private Class SupplierMatch
        Public Property ID As Integer
        Public Property Name As String
        Public Property Phone As String
        Public Overrides Function ToString() As String
            Return If(String.IsNullOrEmpty(Phone), Name, Name & "  (" & Phone & ")")
        End Function
    End Class

    Private Sub ShowNewSupplierSection()
        ClearFormExceptTitle()

        Dim panel As New Panel With {
            .Location = New Point(30, 80),
            .Size = New Size(640, 360),
            .BackColor = Color.White,
            .Padding = New Padding(20)
        }
        Me.Controls.Add(panel)

        Dim lblSectionTitle As New Label With {
            .Text = "New supplier information",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(20, 18),
            .AutoSize = True
        }
        panel.Controls.Add(lblSectionTitle)

        Dim lblPhoneLabel As New Label With {
            .Text = "Phone (optional)",
            .Location = New Point(20, 70),
            .Size = New Size(130, 24),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblPhoneLabel)

        txtNewSupplierPhone = ModernTheme.MakeTextBox()
        txtNewSupplierPhone.Location = New Point(170, 66)
        txtNewSupplierPhone.Width = 260
        txtNewSupplierPhone.Text = savedPhoneNumber
        panel.Controls.Add(txtNewSupplierPhone)

        Dim lblSupName As New Label With {
            .Text = "Supplier name *",
            .Location = New Point(20, 120),
            .Size = New Size(130, 24),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblSupName)

        txtSupplierName = ModernTheme.MakeTextBox()
        txtSupplierName.Location = New Point(170, 116)
        txtSupplierName.Width = 260
        txtSupplierName.Text = savedSupplierName
        panel.Controls.Add(txtSupplierName)

        Dim lblAddress As New Label With {
            .Text = "Address",
            .Location = New Point(20, 170),
            .Size = New Size(130, 24),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblAddress)

        txtAddress = ModernTheme.MakeTextBox()
        txtAddress.Location = New Point(170, 166)
        txtAddress.Width = 260
        txtAddress.Height = 80
        txtAddress.Multiline = True
        panel.Controls.Add(txtAddress)

        Dim btnContinue As Button = ModernTheme.MakePrimaryButton("Continue to add parts")
        btnContinue.Location = New Point(170, 270)
        btnContinue.Width = 170
        AddHandler btnContinue.Click, AddressOf btnContinueToParts_Click
        panel.Controls.Add(btnContinue)

        Dim btnBack As Button = ModernTheme.MakeSecondaryButton("Back")
        btnBack.Location = New Point(360, 270)
        btnBack.Width = 120
        btnBack.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnBack.Click, AddressOf btnBackToSupplierCheck_Click
        panel.Controls.Add(btnBack)
        ModernTheme.RefreshContent(Me)
        FitToContent()
    End Sub

    Private Sub btnContinueToParts_Click(sender As Object, e As EventArgs)
        Dim supplierName As String = txtSupplierName.Text.Trim()
        If String.IsNullOrEmpty(supplierName) Then
            MessageBox.Show("Supplier name is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtSupplierName.Focus()
            Return
        End If

        Dim phoneNumber As String = txtNewSupplierPhone.Text.Trim()
        savedPhoneNumber = phoneNumber
        savedSupplierName = supplierName

        ' Remember the supplier but do not create it yet. It is inserted in the
        ' same transaction as the part, so cancelling now leaves nothing behind.
        pendingSupplierPhone = phoneNumber
        pendingSupplierAddress = txtAddress.Text.Trim()
        currentSupplierName = supplierName
        currentSupplierID = -1
        isNewSupplier = True

        ShowPartEntrySection()
    End Sub

    Private Sub btnBackToSupplierCheck_Click(sender As Object, e As EventArgs)
        ShowSupplierCheckSection()
    End Sub

    Private Sub ShowPartEntrySection()
        ClearFormExceptTitle()

        Dim panel As New Panel With {
            .Location = New Point(30, 80),
            .Size = New Size(640, 470),
            .BackColor = Color.White,
            .Padding = New Padding(20)
        }
        Me.Controls.Add(panel)

        Dim supplierInfo As New Label With {
            .Text = "Supplier: " & currentSupplierName,
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Location = New Point(20, 18),
            .ForeColor = ModernTheme.PrimaryText,
            .AutoSize = True
        }
        panel.Controls.Add(supplierInfo)

        Dim lblPartNumber As New Label With {
            .Text = "Part number",
            .Location = New Point(20, 70),
            .Size = New Size(120, 25),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblPartNumber)

        txtPartNumber = ModernTheme.MakeComboBox()
        txtPartNumber.Location = New Point(150, 66)
        txtPartNumber.Width = 200
        txtPartNumber.DropDownStyle = ComboBoxStyle.DropDown
        txtPartNumber.AutoCompleteMode = AutoCompleteMode.SuggestAppend
        txtPartNumber.AutoCompleteSource = AutoCompleteSource.CustomSource
        EnsurePartNumbersLoaded()
        If allPartNumbers IsNot Nothing Then txtPartNumber.AutoCompleteCustomSource.AddRange(allPartNumbers.ToArray())
        panel.Controls.Add(txtPartNumber)

        Dim btnCheckPart As Button = ModernTheme.MakePrimaryButton("Check part")
        btnCheckPart.Location = New Point(370, 66)
        btnCheckPart.Width = 170
        AddHandler btnCheckPart.Click, AddressOf btnCheckPart_Click
        panel.Controls.Add(btnCheckPart)

        Dim lblDescription As New Label With {
            .Text = "Description",
            .Location = New Point(20, 122),
            .Size = New Size(120, 25),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblDescription)

        txtDescription = ModernTheme.MakeTextBox()
        txtDescription.Location = New Point(150, 118)
        txtDescription.Width = 390
        txtDescription.Height = 80
        txtDescription.Multiline = True
        panel.Controls.Add(txtDescription)

        Dim lblQuantity As New Label With {
            .Text = "Quantity",
            .Location = New Point(20, 220),
            .Size = New Size(120, 25),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblQuantity)

        txtQuantity = ModernTheme.MakeTextBox()
        txtQuantity.Location = New Point(150, 216)
        txtQuantity.Width = 120
        txtQuantity.Text = "1"
        panel.Controls.Add(txtQuantity)

        Dim lblUnitPrice As New Label With {
            .Text = "Unit price",
            .Location = New Point(300, 220),
            .Size = New Size(120, 25),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblUnitPrice)

        txtUnitPrice = ModernTheme.MakeTextBox()
        txtUnitPrice.Location = New Point(420, 216)
        txtUnitPrice.Width = 120
        txtUnitPrice.Text = "0.00"
        panel.Controls.Add(txtUnitPrice)

        Dim lblCategory As New Label With {
            .Text = "Category",
            .Location = New Point(20, 270),
            .Size = New Size(120, 25),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblCategory)

        cmbCategory = New ComboBox With {
            .Location = New Point(150, 266),
            .Size = New Size(160, 25),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 10.0F)
        }
        LoadCategories()
        panel.Controls.Add(cmbCategory)

        Dim lblCondition As New Label With {
            .Text = "Condition",
            .Location = New Point(340, 270),
            .Size = New Size(120, 25),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblCondition)

        cmbCondition = New ComboBox With {
            .Location = New Point(420, 266),
            .Size = New Size(120, 25),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 10.0F)
        }
        LoadConditions()
        panel.Controls.Add(cmbCondition)

        Dim lblPaymentType As New Label With {
            .Text = "Payment type",
            .Location = New Point(20, 310),
            .Size = New Size(120, 25),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        panel.Controls.Add(lblPaymentType)

        cmbPaymentType = New ComboBox With {
            .Location = New Point(150, 306),
            .Size = New Size(160, 25),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 10.0F)
        }
        cmbPaymentType.Items.AddRange(New Object() {"Credit", "Cash", "Bank transfer"})
        cmbPaymentType.SelectedIndex = 0
        panel.Controls.Add(cmbPaymentType)

        Dim btnSubmit As Button = ModernTheme.MakePrimaryButton("Add part")
        btnSubmit.Location = New Point(150, 360)
        btnSubmit.Width = 160
        AddHandler btnSubmit.Click, AddressOf btnSubmit_Click
        panel.Controls.Add(btnSubmit)

        Dim btnCancel As Button = ModernTheme.MakeSecondaryButton("Cancel")
        btnCancel.Location = New Point(330, 360)
        btnCancel.Width = 130
        btnCancel.BackColor = ModernTheme.SurfaceAlt
        btnCancel.ForeColor = ModernTheme.Danger
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        panel.Controls.Add(btnCancel)
        ModernTheme.RefreshContent(Me)
        FitToContent()
    End Sub

    Private Sub EnsurePartNumbersLoaded()
        If allPartNumbers IsNot Nothing Then Return
        allPartNumbers = New List(Of String)
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using cmd As New SqlCommand("SELECT PartNumber FROM mn.SpareParts ORDER BY PartNumber", conn)
                    Using r As SqlDataReader = cmd.ExecuteReader()
                        While r.Read()
                            allPartNumbers.Add(r("PartNumber").ToString())
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load part numbers for autocomplete", ex)
        End Try
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
                            cmbCategory.Items.Add(New CategoryItem With {.ID = reader.GetInt32(0), .Name = reader.GetString(1)})
                        End While
                    End Using
                End Using
                conn.Close()
            End Using
            If cmbCategory.Items.Count > 0 Then cmbCategory.SelectedIndex = 0
        Catch ex As Exception
            MessageBox.Show("Error loading categories: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                            cmbCondition.Items.Add(New ConditionItem With {.ID = reader.GetInt32(0), .Name = reader.GetString(1)})
                        End While
                    End Using
                End Using
                conn.Close()
            End Using
            If cmbCondition.Items.Count > 0 Then cmbCondition.SelectedIndex = 0
        Catch ex As Exception
            MessageBox.Show("Error loading conditions: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
            MessageBox.Show("Please enter a part number", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
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
                            Dim description As String = If(reader.IsDBNull(1), "", reader.GetString(1))
                            Dim categoryID As Integer = If(Not reader.IsDBNull(2), reader.GetInt32(2), 1)
                            txtDescription.Text = description
                            For Each item As CategoryItem In cmbCategory.Items
                                If item.ID = categoryID Then
                                    cmbCategory.SelectedItem = item
                                    Exit For
                                End If
                            Next
                            MessageBox.Show("Part '" & partNumber & "' exists. Updating stock.", "Part Found", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Else
                            txtDescription.Text = ""
                            If cmbCategory.Items.Count > 0 Then cmbCategory.SelectedIndex = 0
                            If cmbCondition.Items.Count > 0 Then cmbCondition.SelectedIndex = 0
                            MessageBox.Show("New part '" & partNumber & "'. Please enter details.", "New Part", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        End If
                    End Using
                End Using
                conn.Close()
            End Using
        Catch ex As Exception
            MessageBox.Show("Error checking part: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnSubmit_Click(sender As Object, e As EventArgs)
        If Not ValidatePartInputs() Then Return

        Dim partNumber As String = txtPartNumber.Text.Trim()
        Dim description As String = txtDescription.Text.Trim()
        Dim quantity As Integer = CInt(txtQuantity.Text)
        Dim unitPrice As Decimal = CDec(txtUnitPrice.Text)
        Dim categoryID As Integer = DirectCast(cmbCategory.SelectedItem, CategoryItem).ID
        Dim conditionID As Integer = DirectCast(cmbCondition.SelectedItem, ConditionItem).ID

        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Dim transaction As SqlTransaction = conn.BeginTransaction()
                Try
                    ' A new supplier becomes real only now, inside the same
                    ' transaction as the stock it is supplying.
                    If isNewSupplier AndAlso currentSupplierID <= 0 Then
                        Dim insertSupplierSql As String = "INSERT INTO mn.Supplier (SupplierName, Phone, Address, CreatedAt) OUTPUT INSERTED.SupplierID VALUES (@name, @phone, @address, GETDATE())"
                        Using cmd As New SqlCommand(insertSupplierSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@name", currentSupplierName)
                            cmd.Parameters.AddWithValue("@phone", If(pendingSupplierPhone.Length = 0, DBNull.Value, CObj(pendingSupplierPhone)))
                            cmd.Parameters.AddWithValue("@address", If(pendingSupplierAddress.Length = 0, DBNull.Value, CObj(pendingSupplierAddress)))
                            currentSupplierID = CInt(cmd.ExecuteScalar())
                        End Using
                        isNewSupplier = False
                    End If

                    Dim checkSql As String = "SELECT PartID, Quantity FROM mn.SpareParts WHERE PartNumber = @partNumber"
                    Dim partID As Integer = 0
                    Dim existingQty As Integer = 0
                    Using cmd As New SqlCommand(checkSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@partNumber", partNumber)
                        Using reader As SqlDataReader = cmd.ExecuteReader()
                            If reader.Read() Then
                                partID = reader.GetInt32(0)
                                existingQty = reader.GetInt32(1)
                            End If
                        End Using
                    End Using

                    If partID > 0 Then
                        Dim newQty As Integer = existingQty + quantity
                        Dim updateSql As String = "UPDATE mn.SpareParts SET Quantity = @qty, UnitPrice = @price, Description = @desc, CategoryID = @catID, ConditionID = @condID WHERE PartNumber = @partNumber"
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
                        Dim insertSql As String = "INSERT INTO mn.SpareParts (PartNumber, Description, CategoryID, ConditionID, SupplierID, Quantity, UnitPrice, CreatedAt) VALUES (@partNum, @desc, @catID, @condID, @supID, @qty, @price, GETDATE())"
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

                    Dim receiptID As Integer
                    Dim isImmediatePayment As Boolean = cmbPaymentType IsNot Nothing AndAlso Not cmbPaymentType.IsDisposed AndAlso cmbPaymentType.SelectedIndex > 0
                    Dim paymentMethod As String = If(cmbPaymentType Is Nothing OrElse cmbPaymentType.IsDisposed, "Credit", cmbPaymentType.Text)
                    Dim totalAmount As Decimal = quantity * unitPrice
                    Dim initialStatus As String = If(isImmediatePayment, "PAID", "UNPAID")
                    Dim initialAmountPaid As Decimal = If(isImmediatePayment, totalAmount, 0D)
                    
                    Dim receiptSql As String = "INSERT INTO mn.PurchaseReceipt (SupplierID, TotalAmount, AmountPaid, Status, Notes) OUTPUT INSERTED.ReceiptID VALUES (@supplierID, @total, @paid, @status, 'Received through Add Spare Part')"
                    Using cmd As New SqlCommand(receiptSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@supplierID", currentSupplierID)
                        cmd.Parameters.AddWithValue("@total", totalAmount)
                        cmd.Parameters.AddWithValue("@paid", initialAmountPaid)
                        cmd.Parameters.AddWithValue("@status", initialStatus)
                        receiptID = Convert.ToInt32(cmd.ExecuteScalar())
                    End Using

                    Dim receiptLineSql As String = "INSERT INTO mn.PurchaseReceiptLine (ReceiptID, PartNumber, Quantity, UnitCost) VALUES (@receiptID, @partNumber, @quantity, @unitCost)"
                    Using cmd As New SqlCommand(receiptLineSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@receiptID", receiptID)
                        cmd.Parameters.AddWithValue("@partNumber", partNumber)
                        cmd.Parameters.AddWithValue("@quantity", quantity)
                        cmd.Parameters.AddWithValue("@unitCost", unitPrice)
                        cmd.ExecuteNonQuery()
                    End Using

                    If isImmediatePayment Then
                        Dim cashAccountCode As String = If(paymentMethod = "Bank transfer", "1001", "1000")
                        AccountingService.PostJournalEntry(conn, transaction, "PURCHASE", receiptID, $"{paymentMethod} purchase - inventory received", New List(Of JournalLineData) From {
                            New JournalLineData With {.AccountCode = "1200", .Debit = totalAmount, .Credit = 0},
                            New JournalLineData With {.AccountCode = cashAccountCode, .Debit = 0, .Credit = totalAmount}
                        })
                        
                        Dim paymentSql As String = "INSERT INTO mn.Payment (Direction, PartyType, PartyID, ReferenceType, ReferenceID, Amount, Method, Notes) VALUES ('OUT', 'SUPPLIER', @partyID, 'PURCHASE_RECEIPT', @refID, @amount, @method, @notes)"
                        Using cmd As New SqlCommand(paymentSql, conn, transaction)
                            cmd.Parameters.AddWithValue("@partyID", currentSupplierID)
                            cmd.Parameters.AddWithValue("@refID", receiptID)
                            cmd.Parameters.AddWithValue("@amount", totalAmount)
                            cmd.Parameters.AddWithValue("@method", paymentMethod)
                            cmd.Parameters.AddWithValue("@notes", $"{paymentMethod} payment at time of purchase")
                            cmd.ExecuteNonQuery()
                        End Using
                    Else
                        AccountingService.PostJournalEntry(conn, transaction, "PURCHASE", receiptID, "Inventory received from supplier on credit", New List(Of JournalLineData) From {
                            New JournalLineData With {.AccountCode = "1200", .Debit = totalAmount, .Credit = 0},
                            New JournalLineData With {.AccountCode = "2000", .Debit = 0, .Credit = totalAmount}
                        })
                    End If

                    Dim batchSql As String = "INSERT INTO mn.PurchaseBatch (PartNumber, SupplierID, PurchaseDate, QuantityPurchased, QuantityRemaining, UnitCost) VALUES (@partNum, @supID, GETDATE(), @qty, @qty, @cost)"
                    Using cmd As New SqlCommand(batchSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@partNum", partNumber)
                        cmd.Parameters.AddWithValue("@supID", currentSupplierID)
                        cmd.Parameters.AddWithValue("@qty", quantity)
                        cmd.Parameters.AddWithValue("@cost", unitPrice)
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim stockSql As String = "INSERT INTO mn.StockCard (PartNumber, MovementType, Quantity, MovementDate, ReferenceType, Remarks) VALUES (@partNum, 'IN', @qty, GETDATE(), 'Purchase', 'Added from spare part form')"
                    Using cmd As New SqlCommand(stockSql, conn, transaction)
                        cmd.Parameters.AddWithValue("@partNum", partNumber)
                        cmd.Parameters.AddWithValue("@qty", quantity)
                        cmd.ExecuteNonQuery()
                    End Using

                    transaction.Commit()
                    suppressCloseConfirmation = True
                    ShowSuccess(partNumber, quantity, unitPrice)
                Catch ex As Exception
                    transaction.Rollback()
                    Throw
                End Try
                conn.Close()
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to save spare part", ex)
            MessageBox.Show("Error saving part: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function ValidatePartInputs() As Boolean
        If String.IsNullOrEmpty(txtPartNumber.Text.Trim()) Then
            MessageBox.Show("Part number is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtPartNumber.Focus()
            Return False
        End If

        If String.IsNullOrEmpty(txtDescription.Text.Trim()) Then
            MessageBox.Show("Description is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtDescription.Focus()
            Return False
        End If

        Dim qty As Integer
        If Not Integer.TryParse(txtQuantity.Text, qty) OrElse qty <= 0 Then
            MessageBox.Show("Quantity must be a positive whole number", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtQuantity.Focus()
            Return False
        End If

        Dim price As Decimal
        If Not Decimal.TryParse(txtUnitPrice.Text, price) OrElse price < 0 Then
            MessageBox.Show("Unit price must be a valid positive number", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtUnitPrice.Focus()
            Return False
        End If

        Return True
    End Function

    Private Sub ShowSuccess(partNumber As String, quantity As Integer, unitPrice As Decimal)
        ClearFormExceptTitle()

        Dim panel As New Panel With {
            .Location = New Point(30, 80),
            .Size = New Size(640, 260),
            .BackColor = Color.White,
            .Padding = New Padding(20)
        }
        Me.Controls.Add(panel)

        Dim lblSuccess As New Label With {
            .Text = "Part '" & partNumber & "' added successfully!",
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .ForeColor = ModernTheme.Success,
            .Location = New Point(20, 20),
            .AutoSize = True
        }
        panel.Controls.Add(lblSuccess)

        Dim lblDetails As New Label With {
            .Text = "Quantity: " & quantity & vbCrLf & "Unit price: " & unitPrice.ToString("C2") & vbCrLf & "Supplier: " & currentSupplierName,
            .Location = New Point(20, 70),
            .Size = New Size(360, 100),
            .Font = New Font("Segoe UI", 11.0F),
            .ForeColor = ModernTheme.PrimaryText
        }
        panel.Controls.Add(lblDetails)

        Dim btnAddAnother As Button = ModernTheme.MakePrimaryButton("Add another")
        btnAddAnother.Location = New Point(20, 190)
        btnAddAnother.Width = 170
        AddHandler btnAddAnother.Click, AddressOf btnAddAnother_Click
        panel.Controls.Add(btnAddAnother)

        Dim btnDone As Button = ModernTheme.MakeSecondaryButton("Done")
        btnDone.Location = New Point(210, 190)
        btnDone.Width = 120
        btnDone.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnDone.Click, AddressOf btnDone_Click
        panel.Controls.Add(btnDone)
        ModernTheme.RefreshContent(Me)
        FitToContent()
    End Sub

    Private Sub btnAddAnother_Click(sender As Object, e As EventArgs)
        ShowPartEntrySection()
    End Sub

    Private Sub btnDone_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    ''' <summary>
    ''' Warns before throwing away typed-in values, so a stray Cancel or the
    ''' window's close box cannot silently discard a part that was being entered.
    ''' The successful-save path sets a flag first and is never prompted.
    ''' </summary>
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If Not suppressCloseConfirmation AndAlso
           e.CloseReason = CloseReason.UserClosing AndAlso
           HasUnsavedInput() Then
            Dim answer As DialogResult = MessageBox.Show(
                Me,
                "Discard the values you have entered?",
                "Discard changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2)
            If answer <> DialogResult.Yes Then
                e.Cancel = True
                Return
            End If
        End If
        MyBase.OnFormClosing(e)
    End Sub

    Private Function HasUnsavedInput() As Boolean
        Return ControlHasText(txtPartNumber) OrElse
               ControlHasText(txtDescription) OrElse
               ControlHasText(txtSupplierName)
    End Function

    ' Sections are rebuilt by disposing the previous controls, so a stale
    ' reference must be treated as empty rather than touched.
    Private Shared Function ControlHasText(box As Control) As Boolean
        If box Is Nothing OrElse box.IsDisposed Then Return False
        Return box.Text.Trim().Length > 0
    End Function
End Class
