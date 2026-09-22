Imports System.Security.Cryptography

Public Module SecurityService
    Private Const StorageAlgorithmName As String = "PBKDF2-SHA256"
    Private Const Iterations As Integer = 120000
    Private Const SaltSize As Integer = 16
    Private Const HashSize As Integer = 32

    Public Function HashPassword(password As String) As String
        Dim salt(SaltSize - 1) As Byte
        Using random As RandomNumberGenerator = RandomNumberGenerator.Create()
            random.GetBytes(salt)
        End Using

        Dim hash As Byte()
        Using derive As New Rfc2898DeriveBytes(password, salt, Iterations, System.Security.Cryptography.HashAlgorithmName.SHA256)
            hash = derive.GetBytes(HashSize)
        End Using

        Return String.Join("$", StorageAlgorithmName, Iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(hash))
    End Function

    Public Function VerifyPassword(password As String, storedValue As String) As Boolean
        If String.IsNullOrWhiteSpace(storedValue) Then Return False
        Dim parts = storedValue.Split("$"c)
        If parts.Length <> 4 OrElse parts(0) <> StorageAlgorithmName Then Return False

        Dim iterations As Integer
        If Not Integer.TryParse(parts(1), iterations) Then Return False
        Try
            Dim salt = Convert.FromBase64String(parts(2))
            Dim expected = Convert.FromBase64String(parts(3))
            Using derive As New Rfc2898DeriveBytes(password, salt, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256)
                Dim actual = derive.GetBytes(expected.Length)
                Return FixedTimeEquals(actual, expected)
            End Using
        Catch
            Return False
        End Try
    End Function

    Public Function IsLegacyHash(storedValue As String) As Boolean
        Return Not String.IsNullOrWhiteSpace(storedValue) AndAlso Not storedValue.StartsWith(StorageAlgorithmName & "$", StringComparison.Ordinal)
    End Function

    Private Function FixedTimeEquals(left As Byte(), right As Byte()) As Boolean
        If left Is Nothing OrElse right Is Nothing OrElse left.Length <> right.Length Then Return False
        Dim difference As Integer = 0
        For index As Integer = 0 To left.Length - 1
            difference = difference Or (left(index) Xor right(index))
        Next
        Return difference = 0
    End Function

    Public Function VerifyLegacySha256(password As String, storedValue As String) As Boolean
        Using sha256 As SHA256 = SHA256.Create()
            Dim hash = Convert.ToBase64String(sha256.ComputeHash(Text.Encoding.UTF8.GetBytes(password)))
            Return String.Equals(hash, storedValue, StringComparison.OrdinalIgnoreCase)
        End Using
    End Function
End Module
