Imports System.Collections.Specialized
Imports System.IO
Imports System.Net
Imports System.Text
Imports Keitai_AA4_Server.util

Module Program
    Dim sjisEnc As Encoding = Encoding.GetEncoding("shift_jis")

    'SERVER INFO
    Dim serverurl As String = "*"
    Dim serverport As String = "80"

    Sub Main(args As String())
        'Setup SJIS
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        Dim listener As New HttpListener()
        listener.Prefixes.Add($"http://{serverurl}:{serverport}/")
        listener.Start()
        Console.WriteLine($"Started AA4 Preinstalled - IMode Server | Yuvi 1.0")
        Console.WriteLine($"Listening on http://{serverurl}:{serverport}/")

        ' Handle requests asynchronously
        HandleRequestsAsync(listener).GetAwaiter().GetResult()
    End Sub

    Private Async Function HandleRequestsAsync(listener As HttpListener) As Task
        While True
            Dim context As HttpListenerContext = Await listener.GetContextAsync()
            ProcessRequestAsync(context)
        End While
    End Function

    Private Async Sub ProcessRequestAsync(context As HttpListenerContext)
        ' Process the request
        Try
            Dim request As HttpListenerRequest = context.Request
            Dim response As HttpListenerResponse = context.Response
            Dim httpContext As HttpListenerContext = CType(context, HttpListenerContext)

            'Parse UID
            Dim queryString As NameValueCollection = request.QueryString
            Dim UID = queryString("uid")
            Dim k = queryString("k")
            Dim ty = queryString("ty")

            'LogIncomingRequest
            Console.WriteLine("Received request:")
            Console.WriteLine($"{httpContext.Request.HttpMethod} {httpContext.Request.Url}")
            Console.WriteLine("Headers:")
            For Each header As String In httpContext.Request.Headers.AllKeys
                Console.WriteLine($"{header}: {httpContext.Request.Headers(header)}")
            Next

            'Start Processing Request
            If request.HttpMethod = "GET" AndAlso request.RawUrl.StartsWith("/i/prein/saiban4/up/appli/saiban4/info.txt") Then
                'Build HexString here
                Dim ResponseString As String = $""
                Dim buffer() As Byte = Encoding.ASCII.GetBytes(ResponseString)
                response.Headers("X-CAPCOM-STATUS") = "OK"
                response.ContentType = "application/octet-stream"
                response.ContentLength64 = buffer.Length
                response.StatusCode = 200
                Dim output As Stream = response.OutputStream
                output.Write(buffer, 0, buffer.Length)
                output.Close()
                Console.WriteLine("Sending response:")
                Console.WriteLine("Sent info.txt Reponse")
                Console.WriteLine()
            ElseIf request.HttpMethod = "GET" AndAlso request.RawUrl.StartsWith("/i/saiban/sreg/isbn_score.php") Then
                Dim hexstring As String = "00"
                Dim dataBytes() As Byte
                If ty = "reg" Then
                    hexstring = "01"
                    dataBytes = HexStringToBytes(hexstring)
                End If
                Dim buffer() As Byte = dataBytes
                response.Headers("X-CAPCOM-STATUS") = "OK"
                response.ContentType = "application/octet-stream"
                response.ContentLength64 = buffer.Length
                response.StatusCode = 200
                Dim output As Stream = response.OutputStream
                output.Write(buffer, 0, buffer.Length)
                output.Close()
                Console.WriteLine("Sending response:")
                Console.WriteLine($"Sent Hex:{hexstring}")
                Console.WriteLine()
            End If
        Catch ex As Exception
            Console.WriteLine("ERROR", "N/A", ex.Message)
        End Try
    End Sub
End Module
