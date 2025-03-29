Imports System
Imports System.Collections.Specialized
Imports System.IO
Imports System.Net
Imports System.Text
Imports Keitai_ItsudemoKeirin_Server.util

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
        Console.WriteLine($"Started Itsudemo Keirin - IMode Server | Yuvi 1.0")
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
            Dim APP = queryString("app")
            Dim VER = queryString("ver")
            Dim REQ = queryString("request")
            Dim POINT = queryString("point")

            'LogIncomingRequest
            Console.WriteLine("Received request:")
            Console.WriteLine($"{httpContext.Request.HttpMethod} {httpContext.Request.Url}")
            Console.WriteLine("Headers:")
            For Each header As String In httpContext.Request.Headers.AllKeys
                Console.WriteLine($"{header}: {httpContext.Request.Headers(header)}")
            Next

            'Start Processing Request
            If request.HttpMethod = "GET" AndAlso request.RawUrl.StartsWith("/i/-appli") Then
                Dim responseString As String
                'Starting with 'error=1&msg=test'
                If REQ = "start" Then
                    responseString = "error=0&point=20000&key=abcd"
                ElseIf REQ = "withdraw" Then
                    responseString = "error=0&point=100"
                ElseIf REQ = "end" Then
                    responseString = "error=0&point=20000&key=abcd"
                ElseIf REQ = "upload" Then
                    responseString = "error=0&point=100"
                End If

                Dim buffer() As Byte = sjisEnc.GetBytes(responseString)
                response.ContentType = "text/html; charset=Shift_JIS"
                response.ContentLength64 = buffer.Length
                response.StatusCode = 200
                Dim output As Stream = response.OutputStream
                output.Write(buffer, 0, buffer.Length)
                output.Close()
                Console.WriteLine("Sending response:")
                Console.WriteLine(responseString)
                Console.WriteLine()
            ElseIf request.HttpMethod = "GET" AndAlso request.RawUrl.StartsWith("/i/ranking") Then
                Dim responseString As String = "Ranking would go here!"
                Dim buffer() As Byte = sjisEnc.GetBytes(responseString)
                response.ContentType = "text/html; charset=Shift_JIS"
                response.ContentLength64 = buffer.Length
                response.StatusCode = 200
                Dim output As Stream = response.OutputStream
                output.Write(buffer, 0, buffer.Length)
                output.Close()
                Console.WriteLine("Sending response:")
                Console.WriteLine(responseString)
                Console.WriteLine()
            End If
        Catch ex As Exception
            Console.WriteLine("ERROR", "N/A", ex.Message)
        End Try
    End Sub
End Module