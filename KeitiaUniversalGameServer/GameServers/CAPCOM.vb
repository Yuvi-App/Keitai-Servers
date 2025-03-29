Imports System.IO
Imports System.Net
Imports UniversalGameServer.Util

Public Class CAPCOM
    Shared ServerName = "Capcom Server"
    Shared DOMAIN = "http://127.0.0.1"
    Shared PORT = "80"
    Shared Sub StartServer()
        Dim listener As New HttpListener()
        listener.Prefixes.Add($"{DOMAIN}:{PORT}/")
        listener.Start()
        Console.WriteLine(vbCrLf)
        Console.WriteLine($"{ServerName} is running on {DOMAIN}:{PORT}/")
        'Create Dir
        Directory.CreateDirectory("MH_I")

        'Handle requests asynchronously
        HandleRequestsAsync(listener).GetAwaiter().GetResult()
    End Sub
    Shared Async Function HandleRequestsAsync(listener As HttpListener) As Task
        While True
            Dim context As HttpListenerContext = Await listener.GetContextAsync()
            ProcessRequestAsync(context)
        End While
    End Function

    Shared Async Sub ProcessRequestAsync(context As HttpListenerContext)
        Try
            Dim request = context.Request
            Dim response = context.Response

            'Extract Out Body and QueryStrings
            Dim QueryStrings = request.QueryString
            Dim UID = QueryStrings("uid")
            Dim TY = QueryStrings("ty")

            'Log Request
            Console.WriteLine("Received request:")
            Console.WriteLine($"{request.HttpMethod} {request.Url}")
            Console.WriteLine("Headers:")
            For Each header As String In request.Headers.AllKeys
                Console.WriteLine($"{header}: {request.Headers(header)}")
            Next

            'Handle Request
            Dim ResponseString As String = ""
            Dim IsHexResponse As Boolean = False
            Dim HTTPMethod = request.HttpMethod
            Dim RAWURL = request.RawUrl.ToLower()
            Select Case HTTPMethod
                Case "GET"
                    If RAWURL.ToLower.Contains("/sreg/isbn_score") Or RAWURL.ToLower.Contains("/sreg/isr") Then
                        IsHexResponse = True
                        ResponseString = "01"
                    ElseIf RAWURL.ToLower.Contains("/ac_check") Then
                        IsHexResponse = False
                        ResponseString = "1"
                    ElseIf RAWURL.ToLower.EndsWith("info.txt") Then
                        IsHexResponse = False
                        ResponseString = "OK"
                    ElseIf RAWURL.Contains("/sh/i/mh/up/appli_904i/") Then 'Monster Hunter just send back any files they request and we find, else send 200
                        ' Try to extract filename from the URL
                        Dim parts = RAWURL.Split("/"c)
                        Dim filename = parts.Last()
                        Dim filepath = Path.Combine("MH_I", filename)
                        If File.Exists(filepath) Then
                            IsHexResponse = True
                            Dim fileBytes = File.ReadAllBytes(filepath)
                            ResponseString += GetFileSizeAsUInt16Hex(filepath)
                            ResponseString += BitConverter.ToString(fileBytes).Replace("-", "")

                            Console.WriteLine($"Sent {filepath}")
                        Else
                            Console.WriteLine("File not found: " & filepath)
                            ResponseString = "" ' No body
                            IsHexResponse = False ' Just to be safe
                        End If

                        'Check for valid Response
                        If ResponseString = "" Then
                            Console.WriteLine($"No ResponseString for {RAWURL}")
                            ' Don't set a default "OK" string — just respond with 200 and empty body
                        End If
                    End If

                Case "POST"
                    If RAWURL = "" Then
                        ' Handle POST case here if needed
                    End If
            End Select

            'Check for valid Response
            If ResponseString = "" Then
                Console.WriteLine($"No ResponseString for {RAWURL}")
                ResponseString = "OK"
            End If

            'Respond to Request
            Dim buffer As Byte()
            Select Case IsHexResponse
                Case False
                    buffer = System.Text.Encoding.GetEncoding(932).GetBytes(ResponseString)
                    response.ContentType = "text/html; charset=Shift_JIS"
                Case True
                    buffer = HexStringToBytes(ResponseString)
                    response.ContentType = "application/octet-stream"
            End Select
            response.Headers("X-CAPCOM-STATUS") = "OK"
            response.StatusCode = 200
            response.ContentLength64 = buffer.Length
            response.OutputStream.Write(buffer, 0, buffer.Length)
            response.OutputStream.Close()
            Console.WriteLine($"{vbCrLf}")
        Catch ex As Exception
            Console.WriteLine("ERROR", "N/A", ex.Message)
        End Try
    End Sub
    Shared Function GetFileSizeAsUInt16Hex(filePath As String) As String
        If Not File.Exists(filePath) Then
            Throw New FileNotFoundException("File not found.", filePath)
        End If

        ' Get file size
        Dim fileSize As Long = New FileInfo(filePath).Length

        ' Ensure the size fits within UInt16 (0 to 65535)
        If fileSize > UShort.MaxValue Then
            Throw New OverflowException("File size exceeds the UInt16 limit.")
        End If

        ' Convert to UInt16 and then to hex
        Dim fileSizeUInt16 As UShort = Convert.ToUInt16(fileSize)
        Return fileSizeUInt16.ToString("X4") ' Format as 4-digit hex
    End Function
End Class
