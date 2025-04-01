Imports System.Data
Imports System.Formats.Asn1.AsnWriter
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading
Imports Keitia_Kobun_Server.HiscoreManager
Module Program
    Sub Main()
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)

        AddHandler AppDomain.CurrentDomain.UnhandledException,
        Sub(sender, e)
            Dim ex = TryCast(e.ExceptionObject, Exception)
            If ex IsNot Nothing Then
                Console.WriteLine("UNHANDLED EXCEPTION: " & ex.Message)
                Console.WriteLine("Stack Trace: " & ex.StackTrace)
            End If
        End Sub

        StartAsync().Wait()
    End Sub

    Private Async Function StartAsync() As Task
        Await CAPCOM.StartServer()
        Console.WriteLine("Kobun fully started.")
        Await Task.Delay(Timeout.Infinite)
    End Function

End Module

Public Class CAPCOM
    Shared ServerName As String = "Kobun"
    Shared DOMAIN As String = "http://+"
    Private Shared isRunning As Boolean = False
    Shared PORT As String = "8088" ' Use non-privileged port unless running as root
    Shared listener As HttpListener

    Public Shared Async Function StartServer() As Task
        Await LoadScoresAsync()

        If isRunning Then Return
        isRunning = True
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

        Try
            Await HandleRequestsAsync(listener)
        Catch ex As Exception
            Console.WriteLine("Listener error: " & ex.Message)
        End Try
    End Function

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
            Dim Score = QueryStrings("s")

            Console.WriteLine("========== NEW REQUEST ==========")
            Console.WriteLine($"Timestamp: {DateTime.Now}")
            Console.WriteLine($"{request.HttpMethod} {request.Url}")
            Console.WriteLine("Client IP: " & GetClientIp(context))
            Console.WriteLine("Headers:")
            For Each header As String In request.Headers.AllKeys
                Console.WriteLine($"{header}: {request.Headers(header)}")
            Next

            Console.WriteLine("Query Parameters:")
            For Each key As String In QueryStrings.AllKeys
                Console.WriteLine($"{key} = {QueryStrings(key)}")
            Next

            Dim ResponseString As String = ""
            Dim IsHexResponse As Boolean = False
            Dim HTTPMethod = request.HttpMethod
            Dim RAWURL = request.RawUrl.ToLower()
            Dim clientIp As String = GetClientIp(context)
            Console.WriteLine($"Request from: {clientIp}")
            Select Case HTTPMethod
                Case "GET"
                    If RAWURL.Contains("circus/sreg/iscore.php") Then
                        Console.WriteLine("Processing: circus/sreg/iscore.php")
                        Console.WriteLine("Scores loaded.")

                        ' Update Score for Player 
                        If Not String.IsNullOrEmpty(Score) Then
                            Console.WriteLine($"Updating score for {clientIp} to {Score}")
                            UpdateScore(clientIp, Score)
                        End If
                        Dim currentYear As Integer = DateTime.Now.Year
                        Dim currentMonth As Integer = DateTime.Now.Month

                        ' Get Player Rank and Score
                        Console.WriteLine("Starting to get Rank Info")
                        Try
                            Console.WriteLine("Getting ATPlayerRank...")
                            Dim ATPlayerRank = GetRankByIp(clientIp)

                            Console.WriteLine("Getting MonthPlayerRank...")
                            Dim MonthPlayerRank = GetRankByMonth(clientIp, currentYear, currentMonth)

                            Console.WriteLine("Getting ATPlayerScore...")
                            Dim ATPlayerScore = GetScore(clientIp)

                            Console.WriteLine("Getting MonthPlayerScore...")
                            Dim MonthPlayerScore = GetScoreByMonth(clientIp, currentYear, currentMonth)

                            Console.WriteLine("Ranks and scores retrieved:")
                            Console.WriteLine($"All-Time Rank: {ATPlayerRank}, Monthly Rank: {MonthPlayerRank}")
                            Console.WriteLine($"All-Time Score: {ATPlayerScore}, Monthly Score: {MonthPlayerScore}")

                            Dim MonthlyTopScore
                            Dim ATTopScore
                            Try
                                MonthlyTopScore = GetTopScoreByMonth(currentYear, currentMonth)
                                ATTopScore = GetTopScore()

                                Console.WriteLine("Top scores retrieved:")
                                Console.WriteLine($"Monthly Top Score: {MonthlyTopScore}, All-Time Top Score: {ATTopScore}")
                            Catch ex As Exception
                                Console.WriteLine("Error while getting top scores: " & ex.Message)
                                Console.WriteLine("Stack Trace: " & ex.StackTrace)
                                Return False
                            End Try

                            ' Compose hex response
                            IsHexResponse = True
                            ResponseString = "02"
                            ResponseString += GetCurrentDateHex()
                            ResponseString += IntToHex4Bytes(MonthPlayerRank)
                            ResponseString += IntToHex4Bytes(MonthPlayerScore)
                            ResponseString += IntToHex4Bytes(MonthlyTopScore)
                            ResponseString += IntToHex4Bytes(ATPlayerRank)
                            ResponseString += IntToHex4Bytes(ATPlayerScore)
                            ResponseString += IntToHex4Bytes(ATTopScore)
                            ResponseString += "00000004" ' Unknown usage
                            Console.WriteLine("Response composed as HEX string.")
                        Catch ex As Exception
                            Console.WriteLine("ERROR while getting rank info: " & ex.Message)
                            Console.WriteLine("Stack Trace: " & ex.StackTrace)
                            Return False
                        End Try


                    End If

                Case "POST"
                    Console.WriteLine("POST method received. No logic implemented yet.")
                    Return False
            End Select

            Dim buffer As Byte()
            If IsHexResponse Then
                buffer = HexStringToBytes(ResponseString)
                response.ContentType = "application/octet-stream"
            Else
                buffer = Encoding.GetEncoding(932).GetBytes(ResponseString)
                response.ContentType = "text/html; charset=Shift_JIS"
            End If

            Console.WriteLine($"Sending response. Content Length: {buffer.Length}")
            response.Headers("X-CAPCOM-STATUS") = "OK"
            response.StatusCode = 200
            response.ContentLength64 = buffer.Length
            Try
                Await response.OutputStream.WriteAsync(buffer, 0, buffer.Length)
                response.OutputStream.Close()
                Console.WriteLine("Response sent successfully.")
            Catch ex As Exception
                Console.WriteLine("Error writing response: " & ex.Message)
                Console.WriteLine("Stack Trace: " & ex.StackTrace)
            End Try
            Console.WriteLine("Response sent successfully.")
        Catch ex As Exception
            Console.WriteLine("ERROR during request processing: " & ex.Message)
            Console.WriteLine("Stack Trace: " & ex.StackTrace)
        End Try
    End Function


    Shared Function GetClientIp(context As HttpListenerContext) As String
        Dim forwardedFor As String = context.Request.Headers("X-Forwarded-For")
        If Not String.IsNullOrEmpty(forwardedFor) Then
            ' Could be a list of IPs: client, proxy1, proxy2...
            Dim ipList = forwardedFor.Split(","c)
            Return ipList(0).Trim() ' First one is the original client
        End If

        Dim realIp As String = context.Request.Headers("X-Real-IP")
        If Not String.IsNullOrEmpty(realIp) Then
            Return realIp.Trim()
        End If

        ' Fallback to direct connection IP
        Return context.Request.RemoteEndPoint.Address.ToString()
    End Function
    Shared Function GetCurrentDateHex() As String
        ' Get current date in yyyyMMdd format
        Dim dateStr As String = DateTime.Now.ToString("yyyyMMdd")

        ' Convert to integer
        Dim dateInt As Integer = Integer.Parse(dateStr)

        ' Convert to 4-byte hex string
        Dim bytes() As Byte = BitConverter.GetBytes(dateInt)

        ' Ensure big-endian byte order (network order)
        If BitConverter.IsLittleEndian Then
            Array.Reverse(bytes)
        End If

        ' Convert each byte to 2-digit hex and join
        Return BitConverter.ToString(bytes).Replace("-", "")
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
    Shared Function IntToHex4Bytes(value As Integer) As String
        Dim bytes() As Byte = BitConverter.GetBytes(value)

        ' Reverse for big-endian format if needed
        If BitConverter.IsLittleEndian Then
            Array.Reverse(bytes)
        End If

        ' Convert each byte to 2-digit hex and join
        Return BitConverter.ToString(bytes).Replace("-", "")
    End Function
End Class
