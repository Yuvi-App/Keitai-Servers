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
                        Dim filename = RAWURL.Split("/"c).Last()
                        Dim fileBytes As Byte() = Nothing
                        Dim description As String = ""

                        ' 1. Try original file first
                        fileBytes = TryReadFile(Path.Combine("MH_I", filename))
                        If fileBytes IsNot Nothing Then
                            description = $"Original: {filename}"
                        Else
                            Console.WriteLine("File not found in OG: " & filename)

                            ' 2. Try recreated file
                            fileBytes = TryReadFile(Path.Combine("MH_I", "recreated", filename))
                            If fileBytes IsNot Nothing Then
                                description = $"Recreated: {filename}"
                            Else
                                Console.WriteLine("File not found in Recreated: " & filename)

                                ' 3. If requested file is pcX_gard_YY.dat and was not found,
                                '    try to find a matching .mbac file of the same weapon type to send as placeholder
                                Dim patternMatch = System.Text.RegularExpressions.Regex.Match(filename, "^(pc\d+_gard)_\d+\.dat$")
                                If patternMatch.Success Then
                                    Dim baseName = patternMatch.Groups(1).Value
                                    Dim placeholderPattern = New System.Text.RegularExpressions.Regex($"^{Regex.Escape(baseName)}.*\.dat$")

                                    ' Search both locations for placeholders
                                    For Each searchDir In {Path.Combine("MH_I"), Path.Combine("MH_I", "recreated")}
                                        If Directory.Exists(searchDir) Then
                                            Dim placeholder = Directory.GetFiles(searchDir)
                                                .Where(Function(f) placeholderPattern.IsMatch(Path.GetFileName(f)))
                                                .OrderBy(Function(f) f)
                                                .FirstOrDefault()
                                            
                                            If placeholder IsNot Nothing Then
                                                fileBytes = TryReadFile(placeholder)
                                                If fileBytes IsNot Nothing Then
                                                    description = $"Placeholder: {placeholder} for {filename}"
                                                    Exit For
                                                End If
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    
                        ' Handle response
                        If fileBytes IsNot Nothing Then
                            IsHexResponse = True
                            ResponseString = BitConverter.ToString(fileBytes).Replace("-", "")
                            Console.WriteLine($"Sent {description}")
                        Else
                            Console.WriteLine($"File not found in any location: {filename}")
                        End If
                    End If

                Case "POST"
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

    ' Helper function that just reads files
    Shared Function TryReadFile(filePath As String) As Byte()
        If File.Exists(filePath) Then
            Return File.ReadAllBytes(filePath)
        End If
        Return Nothing
    End Function
End Class
