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
    Private lblError As Label
    Private pnlActions As Panel

    Private Sub View_StockCard_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ModernTheme.ApplyWindowTheme(Me)
        SetupForm()
        LoadStockMovements("") ' Load all by default
    End Sub

    ' ============ UI SETUP (FULL FIT LAYOUT) ============
    Private Sub SetupForm()
        Me.Text = "Stock Card Audit Trail"
        Me.Size = New Size(1100, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(244, 246, 248)
        ModernTheme.EnableVerticalScroll(Me, 720)

        ' 1. Title
        Dim lblTitle As New Label With {
            .Text = "STOCK CARD - AUDIT TRAIL",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.FromArgb(21, 101, 192),
            .Location = New Point(20, 15),
            .Size = New Size(500, 35)
        }
        Me.Controls.Add(lblTitle)

        ' 2. Search Panel
        Dim pnlSearch As New Panel With {
            .Location = New Point(20, 60),
            .Size = New Size(1045, 60),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.None
        }

        Dim lblPart As New Label With {
            .Text = "Part Number:",
            .Location = New Point(15, 20),
            .Size = New Size(100, 20),
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
        pnlSearch.Controls.Add(lblPart)

        txtPartNumber = New TextBox With {
            .Location = New Point(120, 18),
            .Size = New Size(300, 25),
            .Font = New Font("Segoe UI", 10)
        }
        AddHandler txtPartNumber.KeyPress, AddressOf txtPartNumber_KeyPress
        pnlSearch.Controls.Add(txtPartNumber)

        btnSearch = New Button With {
            .Text = "Search",
            .Location = New Point(430, 15),
            .Size = New Size(120, 32),
            .BackColor = Color.FromArgb(59, 130, 246),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        pnlSearch.Controls.Add(btnSearch)

        btnClear = New Button With {
            .Text = "Clear",
            .Location = New Point(560, 15),
            .Size = New Size(100, 32),
            .BackColor = Color.FromArgb(108, 117, 125),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnClear.Click, AddressOf btnClear_Click
        pnlSearch.Controls.Add(btnClear)
        Me.Controls.Add(pnlSearch)

        ' 3. Error Message
        lblError = New Label With {
            .Location = New Point(20, 125),
            .Size = New Size(1045, 25),
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.Red,
            .Visible = False
        }
        Me.Controls.Add(lblError)

        ' 4. THE TABLE (DataGridView) - FULL FIT
        dgvStockCard = New DataGridView With {
            .Location = New Point(20, 155),
            .Size = New Size(1045, 380),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AllowUserToAddRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle With {.BackColor = Color.FromArgb(245, 247, 250)},
            .Font = New Font("Segoe UI", 10)
        }

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

        ' 5. Bottom Action Panel
        pnlActions = New Panel With {.Location = New Point(20, 545), .Size = New Size(1045, 50), .Visible = False, .Name = "pnlActions"}

        btnExport = New Button With {
            .Text = "Export to CSV", .Location = New Point(0, 5), .Size = New Size(130, 35),
            .BackColor = Color.FromArgb(34, 197, 94), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        AddHandler btnExport.Click, AddressOf btnExport_Click
        pnlActions.Controls.Add(btnExport)

        Dim btnClose As New Button With {
            .Text = "Back to Menu", .Location = New Point(140, 5), .Size = New Size(120, 35),
            .BackColor = Color.White, .ForeColor = Color.Black, .FlatStyle = FlatStyle.Flat
        }
        AddHandler btnClose.Click, Sub() Me.Close()
        pnlActions.Controls.Add(btnClose)

        Me.Controls.Add(pnlActions)
    End Sub

    ' ============ DATA METHODS ============
    Private Sub LoadStockMovements(partNumber As String)
        lblError.Visible = False

        ' Validation
        If Not String.IsNullOrEmpty(partNumber) AndAlso Not PartNumberExists(partNumber) Then
            lblError.Text = "Part Number '" & partNumber & "' not found in Master List."
            lblError.Visible = True
            dgvStockCard.Visible = False
            pnlActions.Visible = False
            Return
        End If

        stockMovementsArray = GetStockMovementsFromDB(partNumber)
        filteredArray = stockMovementsArray

        If filteredArray.Length > 0 Then
            DisplayStockMovements(filteredArray)
            dgvStockCard.Visible = True
            pnlActions.Visible = True
        Else
            lblError.Text = "No transaction records found."
            lblError.Visible = True
            dgvStockCard.Visible = False
            pnlActions.Visible = False
        End If
    End Sub

    Private Function GetStockMovementsFromDB(Optional ByVal partFilter As String = "") As StockMovement()
        Dim list As New List(Of StockMovement)
        Try
            DBModule.OpenConnection()
            Dim query As String = "SELECT MovementDate, PartNumber, " &
                "CASE WHEN MovementType = 'IN' THEN Quantity ELSE 0 END AS InQty, " &
                "CASE WHEN MovementType = 'OUT' THEN Quantity ELSE 0 END AS OutQty, " &
                "ReferenceType, Remarks FROM mn.StockCard"

            If Not String.IsNullOrEmpty(partFilter) Then query &= " WHERE PartNumber = @p"
            query &= " ORDER BY MovementDate ASC"

            Using cmd As New SqlCommand(query, DBModule.Conn)
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
            DBModule.CloseConnection()

            CalculateRunningBalance(list)
        Catch ex As Exception
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
            DBModule.OpenConnection()
            Dim cmd As New SqlCommand("SELECT COUNT(*) FROM mn.SpareParts WHERE PartNumber = @p", DBModule.Conn)
            cmd.Parameters.AddWithValue("@p", partNumber)
            Dim count As Integer = CInt(cmd.ExecuteScalar())
            DBModule.CloseConnection()
            Return count > 0
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