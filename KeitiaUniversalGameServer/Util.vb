Public Class Util
    Shared Function HexStringToBytes(hexString As String) As Byte()
        Dim numberChars As Integer = hexString.Length
        Dim bytes(numberChars / 2 - 1) As Byte
        For i As Integer = 0 To numberChars - 1 Step 2
            bytes(i / 2) = Convert.ToByte(hexString.Substring(i, 2), 16)
        Next
        Return bytes
    End Function
End Class
