Imports System
Imports System.Collections.Specialized
Imports System.IO
Imports System.Net
Imports System.Text
Imports Keitai_KamenRiderPachislot_Sever.util

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
        Console.WriteLine($"Started KamenRider - IMode Server | Yuvi 1.0")
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
            Dim ID = queryString("id")
            Dim data = queryString("data")

            'LogIncomingRequest
            Console.WriteLine("Received request:")
            Console.WriteLine($"{httpContext.Request.HttpMethod} {httpContext.Request.Url}")
            Console.WriteLine("Headers:")
            For Each header As String In httpContext.Request.Headers.AllKeys
                Console.WriteLine($"{header}: {httpContext.Request.Headers(header)}")
            Next

            'Start Processing Request
            If request.HttpMethod = "GET" AndAlso request.RawUrl.StartsWith("/appli/php_v01/auth.php") Then
                Dim AllowedtoPlay = "1" '0=Register Needed 1=OKAY 2=No PersonalData 3=no Usage allowed 4=anonID 6=? 99=DeadServer
                Dim GameVer = "1.0.0" '1.0.0
                Dim SPVer = "1" 'SP ver is 1
                Dim SDVer = "1"
                Dim Rankbattle = "1"
                Dim Daiutistate = "0"

                Dim responseString As String = $"{AllowedtoPlay} {GameVer} {SPVer} {SDVer} {Rankbattle} {Daiutistate}"
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
            ElseIf request.HttpMethod = "GET" AndAlso request.RawUrl.StartsWith("/appli/php_v01/end_daiuti.php") Then
                Dim DaiUtiStatus = "1" '1=Okay 4=Annon ID 99=OutofPoints
                Dim Data0 = "0" 'Ball Payout?
                Dim Data2_3_4 = "202408211837" 'Start Time?
                Dim Data5_6_7 = "202809211837" 'End Time?

                Dim responseString As String = $"{DaiUtiStatus} {Data0} {Data2_3_4} {Data5_6_7}"
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
            ElseIf request.HttpMethod = "GET" AndAlso request.RawUrl.StartsWith("/appli/data/get_dataB.php") Then
                Dim responseString As String = $""
                Select Case data
                    Case 0
                        responseString = $"05000001"
                    Case 1
                        responseString = $"C1000016"
                    Case 2
                        responseString = $"2100000f"
                    Case 3
                        responseString = $"DD000003"
                    Case 4
                        responseString = $"03000006"
                    Case 5
                        responseString = $"17000005"
                End Select

                'Build HexString here
                Dim hexData As String = responseString

                ' Convert hex string to byte array
                Dim dataBytes() As Byte = HexStringToBytes(hexData)
                Dim buffer() As Byte = dataBytes
                response.ContentType = "application/octet-stream"
                response.ContentLength64 = buffer.Length
                response.StatusCode = 200
                Dim output As Stream = response.OutputStream
                output.Write(buffer, 0, buffer.Length)
                output.Close()
                Console.WriteLine("Sending response:")
                Console.WriteLine(hexData)
                Console.WriteLine()
            End If
        Catch ex As Exception
            Console.WriteLine("ERROR", "N/A", ex.Message)
        End Try
    End Sub
End Module
