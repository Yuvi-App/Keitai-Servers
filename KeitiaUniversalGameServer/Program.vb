Imports System
Imports System.Threading
Imports System.Net
Imports System.Text

Module Program
    Dim CurrentAppVer = "0.1"
    Sub Main(args As String())
        'Setup SJIS
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)

        'Start App
        Console.WriteLine($"Keitai Universal Game Server - Yuvi V{CurrentAppVer}")
        Console.WriteLine("-----------------------------------")

        Dim gameServers As New List(Of GameServer) From {
            New GameServer("Capcom Server", AddressOf CAPCOM.StartServer)
        }

        While True
            Console.WriteLine("Available Game Servers:")
            For i As Integer = 0 To gameServers.Count - 1
                Console.WriteLine($"{i + 1}. {gameServers(i).Name}")
            Next

            Console.Write("Select a server to start (or type 'exit' to quit): ")
            Dim input As String = Console.ReadLine()

            If input.ToLower() = "exit" Then
                Exit While
            End If

            If Integer.TryParse(input, result:=Nothing) AndAlso CInt(input) > 0 AndAlso CInt(input) <= gameServers.Count Then
                Dim selectedServer As GameServer = gameServers(CInt(input) - 1)
                Dim thread As New Thread(AddressOf selectedServer.StartServer)
                thread.IsBackground = True
                thread.Start()
                Console.WriteLine($"{selectedServer.Name} is starting...")
            Else
                Console.WriteLine("Invalid selection. Please try again.")
            End If
        End While
        Console.WriteLine("Exiting application.")
    End Sub

    ' Game Server Class for Extensibility
    Class GameServer
        Public Property Name As String
        Private ReadOnly _startServer As Action

        Public Sub New(name As String, startServer As Action)
            Me.Name = name
            _startServer = startServer
        End Sub

        Public Sub StartServer()
            _startServer.Invoke()
        End Sub
    End Class


End Module
