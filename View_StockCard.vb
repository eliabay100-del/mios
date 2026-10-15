Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Collections.Generic
Imports System.Text
Imports System.IO

Public Class View_StockCard
    ' ============ DATA STRUCTURE ============
    Public Structure StockMovement
        Public MovementDate As DateTime
        Public PartNumber As String
        Public InQty As Integer
        Public OutQty As Integer
        Public Balance As Integer
        Public Reference As String
        Public Remarks As String
    End Structure

    Private stockMovementsArray() As StockMovement
    Private filteredArray() As StockMovement

    ' UI Controls
    Private WithEvents dgvStockCard As DataGridView
    Private txtPartNumber As TextBox
    Private btnSearch As Button
    Private btnClear As Button
    Private btnExport As Button
    Private btnClose As Button
    Private lblError As Label

    Private Sub View_StockCard_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ModernTheme.ApplyWindowTheme(Me)
        SetupForm()
        LoadStockMovements("") ' Load all by default
    End Sub

    ' ============ UI SETUP (FULL FIT LAYOUT) ============
    Private Sub SetupForm()
        Me.Text = "Stock Card Audit Trail"
        Me.Size = New Size(1080, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        ModernTheme.EnableVerticalScroll(Me, 700)

        ' 1. Title
        Dim lblTitle As New Label With {
            .Text = "Stock card audit trail",
            .Font = New Font("Segoe UI", 24, FontStyle.Bold),
            .ForeColor = ModernTheme.PrimaryText,
            .Location = New Point(22, 18),
            .Size = New Size(400, 34)
        }
        Me.Controls.Add(lblTitle)

        ' 2. Search Panel
        Dim pnlSearch As New Panel With {
            .Location = New Point(22, 64),
            .Size = New Size(1010, 52),
            .BackColor = Color.White
        }

        Dim lblPart As New Label With {
            .Text = "Part Number:",
            .Location = New Point(12, 8),
            .Size = New Size(100, 30),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ModernTheme.MutedText
        }
        pnlSearch.Controls.Add(lblPart)

        txtPartNumber = ModernTheme.MakeTextBox()
        txtPartNumber.Location = New Point(120, 8)
        txtPartNumber.Width = 300
        AddHandler txtPartNumber.KeyPress, AddressOf txtPartNumber_KeyPress
        pnlSearch.Controls.Add(txtPartNumber)

        btnSearch = ModernTheme.MakePrimaryButton("Search")
        btnSearch.Location = New Point(430, 8)
        btnSearch.Width = 120
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlSearch.Controls.Add(btnSearch)

        btnClear = ModernTheme.MakeSecondaryButton("Clear all")
        btnClear.Location = New Point(560, 8)
        btnClear.Width = 120
        btnClear.BackColor = ModernTheme.SurfaceAlt
        AddHandler btnClear.Click, AddressOf btnClear_Click
        pnlSearch.Controls.Add(btnClear)
        Me.Controls.Add(pnlSearch)

        ' 3. Error Message
        lblError = New Label With {
            .Location = New Point(22, 130),
            .Size = New Size(1010, 25),
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.Red,
            .Visible = False
        }
        Me.Controls.Add(lblError)

        ' 4. THE TABLE (DataGridView) - FULL FIT
        dgvStockCard = New DataGridView With {
            .Location = New Point(22, 160),
            .Size = New Size(1010, 360),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle With {.BackColor = ModernTheme.SurfaceAlt},
            .Font = New Font("Segoe UI", 10)
        }
        ModernTheme.StyleGrid(dgvStockCard)

        dgvStockCard.Columns.Add("MovementDate", "Date")
        dgvStockCard.Columns.Add("PartNumber", "Part Number")
        dgvStockCard.Columns.Add("InQty", "In")
        dgvStockCard.Columns.Add("OutQty", "Out")
        dgvStockCard.Columns.Add("Balance", "Balance")
        dgvStockCard.Columns.Add("Reference", "Reference")
        dgvStockCard.Columns.Add("Remarks", "Remarks")

        ' Adjust column weight for "Full Fit"
        dgvStockCard.Columns("MovementDate").FillWeight = 80
        dgvStockCard.Columns("PartNumber").FillWeight = 100
        dgvStockCard.Columns("InQty").FillWeight = 50
        dgvStockCard.Columns("OutQty").FillWeight = 50
        dgvStockCard.Columns("Balance").FillWeight = 60
        dgvStockCard.Columns("Reference").FillWeight = 100
        dgvStockCard.Columns("Remarks").FillWeight = 180

        ' Alignment
        dgvStockCard.Columns("InQty").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvStockCard.Columns("OutQty").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvStockCard.Columns("Balance").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        AddHandler dgvStockCard.CellFormatting, AddressOf dgvStockCard_CellFormatting
        Me.Controls.Add(dgvStockCard)

        ' 5. Bottom action row
        btnExport = ModernTheme.MakePrimaryButton("Export to CSV")
        btnExport.Location = New Point(870, 570)
        btnExport.Width = 160
        AddHandler btnExport.Click, AddressOf btnExport_Click
        Me.Controls.Add(btnExport)

        btnClose = ModernTheme.MakeSecondaryButton("Back to dashboard")
        btnClose.Location = New Point(22, 570)
        btnClose.Width = 180
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)
    End Sub

    ' ============ DATA METHODS ============
    Private Sub LoadStockMovements(partNumber As String)
        lblError.Visible = False

        ' Validation
        If Not String.IsNullOrEmpty(partNumber) AndAlso Not PartNumberExists(partNumber) Then
            lblError.Text = "Part Number '" & partNumber & "' not found in Master List."
            lblError.Visible = True
            dgvStockCard.Visible = False
            btnExport.Visible = False
            btnClose.Visible = False
            Return
        End If

        stockMovementsArray = GetStockMovementsFromDB(partNumber)
        filteredArray = stockMovementsArray

        If filteredArray.Length > 0 Then
            DisplayStockMovements(filteredArray)
            dgvStockCard.Visible = True
            btnExport.Visible = True
            btnClose.Visible = True
        Else
            lblError.Text = "No transaction records found."
            lblError.Visible = True
            dgvStockCard.Visible = False
            btnExport.Visible = False
            btnClose.Visible = False
        End If
    End Sub

    Private Function GetStockMovementsFromDB(Optional ByVal partFilter As String = "") As StockMovement()
        Dim list As New List(Of StockMovement)
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Dim query As String = "SELECT MovementDate, PartNumber, " &
                    "CASE WHEN MovementType = 'IN' THEN Quantity ELSE 0 END AS InQty, " &
                    "CASE WHEN MovementType = 'OUT' THEN Quantity ELSE 0 END AS OutQty, " &
                    "ReferenceType, Remarks FROM mn.StockCard"

                If Not String.IsNullOrEmpty(partFilter) Then query &= " WHERE PartNumber = @p"
                query &= " ORDER BY MovementDate ASC"

                Using cmd As New SqlCommand(query, conn)
                    If Not String.IsNullOrEmpty(partFilter) Then cmd.Parameters.AddWithValue("@p", partFilter)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(New StockMovement With {
                                .MovementDate = CDate(reader("MovementDate")),
                                .PartNumber = reader("PartNumber").ToString(),
                                .InQty = CInt(reader("InQty")),
                                .OutQty = CInt(reader("OutQty")),
                                .Reference = reader("ReferenceType").ToString(),
                                .Remarks = If(reader("Remarks") Is DBNull.Value, "", reader("Remarks").ToString())
                            })
                        End While
                    End Using
                End Using

                CalculateRunningBalance(list)
            End Using
        Catch ex As Exception
            AppLogger.Error("Failed to load stock movements", ex)
            MessageBox.Show("Database Connection Error: " & ex.Message)
        End Try
        Return list.ToArray()
    End Function

    Private Sub CalculateRunningBalance(ByRef list As List(Of StockMovement))
        Dim bal As Integer = 0
        For i As Integer = 0 To list.Count - 1
            Dim m = list(i)
            bal += (m.InQty - m.OutQty)
            If bal < 0 Then bal = 0
            m.Balance = bal
            list(i) = m
        Next
    End Sub

    Private Sub DisplayStockMovements(movements As StockMovement())
        dgvStockCard.Rows.Clear()
        For Each m In movements
            dgvStockCard.Rows.Add(m.MovementDate.ToString("yyyy-MM-dd"), m.PartNumber, m.InQty, m.OutQty, m.Balance, m.Reference, m.Remarks)
        Next
    End Sub

    Private Function PartNumberExists(partNumber As String) As Boolean
        Try
            Using conn As New SqlConnection(DBModule.ConnString)
                conn.Open()
                Using cmd As New SqlCommand("SELECT COUNT(*) FROM mn.SpareParts WHERE PartNumber = @p", conn)
                    cmd.Parameters.AddWithValue("@p", partNumber)
                    Dim count As Integer = CInt(cmd.ExecuteScalar())
                    Return count > 0
                End Using
            End Using
        Catch
            Return False
        End Try
    End Function

    ' ============ EVENT HANDLERS ============
    Private Sub dgvStockCard_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex >= 0 Then
            ' Color code In (Green) / Out (Red)
            If e.ColumnIndex = 2 AndAlso CInt(dgvStockCard.Rows(e.RowIndex).Cells("InQty").Value) > 0 Then
                e.CellStyle.ForeColor = Color.Green
                e.CellStyle.Font = New Font(dgvStockCard.Font, FontStyle.Bold)
            End If
            If e.ColumnIndex = 3 AndAlso CInt(dgvStockCard.Rows(e.RowIndex).Cells("OutQty").Value) > 0 Then
                e.CellStyle.ForeColor = Color.Red
                e.CellStyle.Font = New Font(dgvStockCard.Font, FontStyle.Bold)
            End If
        End If
    End Sub

    Private Sub btnSearch_Click(sender As Object, e As EventArgs)
        LoadStockMovements(txtPartNumber.Text.Trim())
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs)
        txtPartNumber.Clear()
        LoadStockMovements("")
    End Sub

    Private Sub txtPartNumber_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            btnSearch_Click(Nothing, Nothing)
            e.Handled = True
        End If
    End Sub

    ' ============ CSV EXPORT LOGIC ============
    Private Sub btnExport_Click(sender As Object, e As EventArgs)
        If filteredArray Is Nothing OrElse filteredArray.Length = 0 Then Return

        Using sfd As New SaveFileDialog()
            sfd.Filter = "CSV Files (*.csv)|*.csv"
            sfd.FileName = "StockCard_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".csv"

            If sfd.ShowDialog() = DialogResult.OK Then
                Dim sb As New StringBuilder()
                sb.AppendLine("Date,PartNumber,In,Out,Balance,Reference,Remarks")

                For Each m In filteredArray
                    sb.AppendLine(String.Format("{0},{1},{2},{3},{4},{5},{6}",
                        m.MovementDate.ToString("yyyy-MM-dd"), m.PartNumber, m.InQty, m.OutQty, m.Balance, m.Reference, m.Remarks))
                Next

                File.WriteAllText(sfd.FileName, sb.ToString())
                MessageBox.Show("Data exported successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub
End Class
