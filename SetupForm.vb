Public Class SetupForm
    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        UserSetup.CreateInitialUser()
    End Sub
End Class