Imports System.IO
Imports System.Net
Imports System.Text
Imports UniversalGameServer.Util
Imports System.Runtime.InteropServices
Module Program
    Sub Main()
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)

        ' Start the Monster Hunter I server
        CAPCOM.StartServer()

        ' Prevent the app from closing
        Console.WriteLine("Press Enter to stop the server.")
        Console.ReadLine()
    End Sub
End Module

Public Class CAPCOM
    Shared ServerName As String = "Monster Hunter I"
    Shared DOMAIN As String = "http://*"
    Shared PORT As String = "8087" ' Use non-privileged port unless running as root
    Shared listener As HttpListener

    Public Shared Async Sub StartServer()
        If Not HttpListener.IsSupported Then
            Console.WriteLine("HttpListener is not supported on this platform.")
            Return
        End If

        listener = New HttpListener()

        ' Ensure correct prefix
        Dim prefix = $"{DOMAIN}:{PORT}/"
        listener.Prefixes.Add(prefix)

        Try
            listener.Start()
        Catch ex As HttpListenerException
            Console.WriteLine("Failed to start listener: " & ex.Message)
            Return
        End Try

        Console.WriteLine(vbCrLf & $"{ServerName} is running on {prefix}")

        ' Create directory
        Directory.CreateDirectory("MH_I")

        Try
            Await HandleRequestsAsync(listener)
        Catch ex As Exception
            Console.WriteLine("Listener error: " & ex.Message)
        End Try
    End Sub

    Shared Async Function HandleRequestsAsync(listener As HttpListener) As Task
        While listener.IsListening
            Try
                Dim context As HttpListenerContext = Await listener.GetContextAsync()
                Dim ProcessRequest = Task.Run(Async Function() Await ProcessRequestAsync(context))
            Catch ex As HttpListenerException
                ' Listener stopped or broke
                Exit While
            End Try
        End While
    End Function

    Shared Async Function ProcessRequestAsync(context As HttpListenerContext) As Task(Of Boolean)
        Try
            Dim request = context.Request
            Dim response = context.Response

            ' Extract query
            Dim QueryStrings = request.QueryString
            Dim UID = QueryStrings("uid")
            Dim TY = QueryStrings("ty")

            Console.WriteLine("Received request:")
            Console.WriteLine($"{request.HttpMethod} {request.Url}")
            Console.WriteLine("Headers:")
            For Each header As String In request.Headers.AllKeys
                Console.WriteLine($"{header}: {request.Headers(header)}")
            Next

            Dim ResponseString As String = ""
            Dim IsHexResponse As Boolean = False
            Dim HTTPMethod = request.HttpMethod
            Dim RAWURL = request.RawUrl.ToLower()

            Select Case HTTPMethod
                Case "GET"
                    If RAWURL.Contains("sreg/imh_sreg.php") And TY = Nothing Then
                        IsHexResponse = True
                        ResponseString = "01000000"
                        '01 = g2g
                        '6e =membership required
                    ElseIf RAWURL.Contains("sreg/imh_sreg.php") And TY = "load" Then
                        IsHexResponse = True
                        ResponseString = "8C000000"
                        '8c = g2g
                        '6e =membership required
                    ElseIf RAWURL.Contains("/ac_check") Then
                        ResponseString = "1"
                    ElseIf RAWURL.EndsWith("info.txt") Then
                        ResponseString = "OK"
                    ElseIf RAWURL.Contains("/sh/i/mh/up/appli_904i/") Then
                        Dim parts = RAWURL.Split("/"c)
                        Dim filename = parts.Last()
                        Dim filepath = Path.Combine("MH_I", filename)

                        If File.Exists(filepath) Then
                            IsHexResponse = True
                            Dim fileBytes = File.ReadAllBytes(filepath)
                            ResponseString += BitConverter.ToString(fileBytes).Replace("-", "")

                            Console.WriteLine($"Sent {filepath}")
                        Else
                            Console.WriteLine("File not found: " & filepath)
                        End If
                    End If

                Case "POST"
                    ' Add POST handling if needed
            End Select

            Dim buffer As Byte()
            If IsHexResponse Then
                buffer = HexStringToBytes(ResponseString)
                response.ContentType = "application/octet-stream"
            Else
                buffer = Encoding.GetEncoding(932).GetBytes(ResponseString)
                response.ContentType = "text/html; charset=Shift_JIS"
            End If

            response.Headers("X-CAPCOM-STATUS") = "OK"
            response.StatusCode = 200
            response.ContentLength64 = buffer.Length
            Await response.OutputStream.WriteAsync(buffer, 0, buffer.Length)
            response.OutputStream.Close()
        Catch ex As Exception
            Console.WriteLine("ERROR: " & ex.Message)
        End Try
    End Function

    Shared Function GetFileSizeAsUInt16HexLE(filePath As String) As String
        If Not File.Exists(filePath) Then Throw New FileNotFoundException("File not found.", filePath)
        Dim fileSize As Long = New FileInfo(filePath).Length
        If fileSize > UShort.MaxValue Then Throw New OverflowException("File size exceeds the UInt16 limit.")
        Dim bytes() As Byte = BitConverter.GetBytes(Convert.ToUInt16(fileSize))
        Return bytes(0).ToString("X2") & bytes(1).ToString("X2")
    End Function

    Shared Function HexStringToBytes(hex As String) As Byte()
        Dim cleanedHex = hex.Replace(" ", "").Replace("-", "")
        Dim bytes(cleanedHex.Length \ 2 - 1) As Byte
        For i = 0 To bytes.Length - 1
            bytes(i) = Convert.ToByte(cleanedHex.Substring(i * 2, 2), 16)
        Next
        Return bytes
    End Function
End Class
