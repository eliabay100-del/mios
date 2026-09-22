Imports System.Windows.Forms

Public Module LanguageService
    Public Event LanguageChanged As EventHandler
    Public Property IsAmharic As Boolean = False

    Private ReadOnly translations As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
        {"Inventory Management", "የእቃ አስተዳደር"}, {"Operations dashboard", "የስራ አስተዳደር ዳሽቦርድ"},
        {"Add Spare Part", "የመለዋወጫ እቃ ጨምር"}, {"Issue Spare Part", "የመለዋወጫ እቃ አውጣ"},
        {"Inventory List", "የእቃ ዝርዝር"}, {"Purchase Batches", "የግዢ ዝርዝር"},
        {"Sold Items", "የተሸጡ እቃዎች"}, {"Stock Card", "የእቃ እንቅስቃሴ"},
        {"Suppliers", "አቅራቢዎች"}, {"Customers", "ደንበኞች"}, {"Record Payment", "ክፍያ መዝግብ"},
        {"Users & Roles", "ተጠቃሚዎች እና ሚናዎች"}, {"Manage Categories", "ምድቦችን አስተዳድር"},
        {"Adjust Stock", "የእቃ ማስተካከያ"}, {"Financial Reports", "የፋይናንስ ሪፖርቶች"},
        {"Record Expense", "ወጪ መዝግብ"}, {"Process Return", "መመለሻ አስኬድ"},
        {"Database Backup", "የዳታቤዝ ባክአፕ"}, {"Exit", "ውጣ"}, {"Stock on hand", "በእጅ ያለ እቃ"},
        {"Low inventory", "ዝቅተኛ እቃ"}, {"Sales today", "የዛሬ ሽያጭ"},
        {"Current quantity", "አሁን ያለ መጠን"}, {"Recorded batches", "የተመዘገቡ ግዢዎች"},
        {"Parts at or below 5", "ከ5 ወይም ከዚያ በታች"}, {"Current price total", "የአሁኑ ዋጋ ድምር"}
    }

    Public Sub Toggle()
        IsAmharic = Not IsAmharic
        RaiseEvent LanguageChanged(Nothing, EventArgs.Empty)
    End Sub

    Public Function Translate(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return value
        For Each pair In translations
            If IsAmharic AndAlso String.Equals(value, pair.Key, StringComparison.OrdinalIgnoreCase) Then Return pair.Value
            If Not IsAmharic AndAlso value = pair.Value Then Return pair.Key
        Next
        Return value
    End Function

    Public Sub ApplyTo(root As Control)
        If TypeOf root Is Button OrElse TypeOf root Is Label OrElse TypeOf root Is Form Then root.Text = Translate(root.Text)
        For Each child As Control In root.Controls
            ApplyTo(child)
        Next
    End Sub
End Module
