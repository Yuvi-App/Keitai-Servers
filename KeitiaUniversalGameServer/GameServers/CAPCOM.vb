Imports System.Net
Imports UniversalGameServer.Util

Public Class CAPCOM
    Shared ServerName = "TEMPLATE"
    Shared DOMAIN = "http://localhost"
    Shared PORT = "80"
    Shared Sub StartServer()
        Dim listener As New HttpListener()
        listener.Prefixes.Add($"{DOMAIN}:{PORT}/")
        listener.Start()
        Console.WriteLine($"{ServerName} is running on {DOMAIN}:{PORT}/")
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
            Dim RAWURL = request.RawUrl
            Select Case HTTPMethod
                Case "GET"
                    If RAWURL.ToLower.Contains("/sreg/isbn_score.php") Then
                        IsHexResponse = True
                        ResponseString = "01"
                    ElseIf RAWURL.ToLower.EndsWith("info.txt") Then
                        IsHexResponse = False
                        ResponseString = "OK"
                    End If

                Case "POST"
                    If RAWURL.ToLower = "" Then

                    End If
            End Select

            'Check for valid Response
            If ResponseString = "" Then
                Console.WriteLine($"No ReponseString for {RAWURL}")
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
            response.ContentLength64 = Buffer.Length
            response.OutputStream.Write(Buffer, 0, Buffer.Length)
            response.OutputStream.Close()
        Catch ex As Exception
            Console.WriteLine("ERROR", "N/A", ex.Message)
        End Try
    End Sub
End Class
