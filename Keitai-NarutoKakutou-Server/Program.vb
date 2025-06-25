Imports System.Collections.Specialized
Imports System.IO
Imports System.Net
Imports System.Text
Imports Keitai_NarutoKakutou_Server.util

Module Program
    Dim sjisEnc As Encoding = Encoding.GetEncoding("shift_jis")

    'SERVER INFO
    Dim serverurl As String = "*"
    Dim serverport As String = "8090"

    Sub Main(args As String())
        'Setup SJIS
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        Dim listener As New HttpListener()
        listener.Prefixes.Add($"http://{serverurl}:{serverport}/")
        listener.Start()
        Console.WriteLine($"Started Naruto Kakutou - IMode Server | Yuvi 1.0")
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
            Dim type = queryString("type")

            'LogIncomingRequest
            Console.WriteLine("Received request:")
            Console.WriteLine($"{httpContext.Request.HttpMethod} {httpContext.Request.Url}")
            Console.WriteLine("Headers:")
            For Each header As String In httpContext.Request.Headers.AllKeys
                Console.WriteLine($"{header}: {httpContext.Request.Headers(header)}")
            Next

            'Start Processing Request
            If request.HttpMethod = "GET" AndAlso request.RawUrl.StartsWith("/app/rpg2.php") Then
                Dim ResponseString As String
                Dim buffer() As Byte
                Select Case type
                    Case "check"
                        'Build HexString here
                        Dim UserID = "1337"
                        Dim Username = "TestUser"
                        Dim UsernameCount = Username.Length
                        Dim user_regi = 0
                        'Format byte Array as such, userid,single byte length of username,username, and user regi

                        buffer = BuildUserByteArray(UserID, Username, user_regi)
                        response.ContentType = "application/x-www-form-urlencoded"
                    Case "start"
                        ResponseString = "014112340504030201160050007200650073007300200045004e005400450052"
                        buffer = HexStringToBytes(ResponseString)
                        response.ContentType = "application/x-www-form-urlencoded"
                    Case "resource"
                        ResponseString = "014112340504030201160050007200650073007300200045004e005400450052"
                        buffer = HexStringToBytes(ResponseString)
                        response.ContentType = "application/x-www-form-urlencoded"
                End Select
                response.ContentLength64 = buffer.Length
                response.StatusCode = 200
                Dim output As Stream = response.OutputStream
                output.Write(buffer, 0, buffer.Length)
                output.Close()
                Console.WriteLine("Sending response:")
                Console.WriteLine($"{ResponseString}")
                Console.WriteLine()

            ElseIf request.HttpMethod = "POST" AndAlso request.RawUrl.StartsWith("/app/rpg2.php") Then
                Dim ResponseString As String
                Dim buffer() As Byte
                Select Case type
                    Case "updateinfo"
                        ResponseString = "0,0"
                        buffer = Encoding.ASCII.GetBytes(ResponseString)
                        response.ContentType = "application/x-www-form-urlencoded"
                End Select
                response.ContentLength64 = buffer.Length
                response.StatusCode = 200
                Dim output As Stream = response.OutputStream
                output.Write(buffer, 0, buffer.Length)
                output.Close()
                Console.WriteLine("Sending response:")
                Console.WriteLine($"{ResponseString}")
                Console.WriteLine()
            End If
        Catch ex As Exception
            Console.WriteLine("ERROR", "N/A", ex.Message)
        End Try
    End Sub

    Public Function BuildUserByteArray(UserID As String, Username As String, user_regi As Integer) As Byte()
        Dim byteList As New List(Of Byte)()

        ' Convert UserID string to bytes (e.g., ASCII encoding)
        byteList.AddRange(System.Text.Encoding.ASCII.GetBytes(UserID))

        ' Add Username length (single byte)
        Dim usernameLength As Byte = CByte(Username.Length)
        byteList.Add(usernameLength)

        ' Add Username bytes
        byteList.AddRange(System.Text.Encoding.ASCII.GetBytes(Username))

        ' Add user_regi as 4-byte little-endian integer
        byteList.AddRange(BitConverter.GetBytes(user_regi))

        Return byteList.ToArray()
    End Function
End Module
