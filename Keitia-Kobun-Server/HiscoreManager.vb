Imports System.IO
Imports System.Text.Json

Public Class ScoreEntry
    Public Property score As Integer
    Public Property timestamp As DateTime
End Class

Public Class HiscoreManager
    Shared hiscoreFile As String = Path.Combine(AppContext.BaseDirectory, "hiscores.json")
    Shared scores As New Dictionary(Of String, ScoreEntry)

    Shared Async Function LoadScoresAsync() As Task
        Try
            If File.Exists(hiscoreFile) Then
                Console.WriteLine("📄 Reading score file contents:")
                Console.WriteLine(File.ReadAllText(hiscoreFile))
                Dim json As String = Await File.ReadAllTextAsync(hiscoreFile)
                Dim parsed = JsonSerializer.Deserialize(Of Dictionary(Of String, ScoreEntry))(json)
                If parsed IsNot Nothing Then
                    scores = parsed
                Else
                    scores = New Dictionary(Of String, ScoreEntry)()
                End If
                If scores Is Nothing Then
                    Console.WriteLine("Scores dictionary is still null!")
                Else
                    Console.WriteLine($"Scores loaded: {scores.Count} entries")
                End If
            Else
                scores = New Dictionary(Of String, ScoreEntry)()
            End If
        Catch ex As Exception
            Console.WriteLine("Error loading scores: " & ex.Message)
            scores = New Dictionary(Of String, ScoreEntry)()
        End Try
    End Function
    Shared Async Function SaveScoresAsync() As Task
        Try
            Dim json As String = JsonSerializer.Serialize(scores, New JsonSerializerOptions With {.WriteIndented = True})
            Await File.WriteAllTextAsync(hiscoreFile, json)
        Catch ex As Exception
            Console.WriteLine("Error saving scores: " & ex.Message)
        End Try
    End Function
    Shared Function GetScore(ip As String) As Integer
        If scores Is Nothing Then Return 0
        If scores.ContainsKey(ip) Then
            Return scores(ip).score
        End If
        Return 0
    End Function
    Shared Function GetScoreByMonth(ip As String, year As Integer, month As Integer) As Integer
        If scores.ContainsKey(ip) Then
            Dim entry = scores(ip)
            If entry.timestamp.Year = year And entry.timestamp.Month = month Then
                Return entry.score
            End If
        End If
        Return 0
    End Function
    Shared Function GetRankByMonth(ip As String, year As Integer, month As Integer) As Integer
        ' Filter only scores that match the specified month and year
        Dim monthlyScores = scores.
        Where(Function(pair) pair.Value.timestamp.Year = year AndAlso pair.Value.timestamp.Month = month).
        OrderByDescending(Function(pair) pair.Value.score).
        ToList()

        ' Find the index of the IP
        For i As Integer = 0 To monthlyScores.Count - 1
            If monthlyScores(i).Key = ip Then
                Return i + 1 ' 1-based rank
            End If
        Next

        Return 0 ' IP not ranked this month
    End Function

    Shared Async Sub UpdateScore(ip As String, newScore As Integer)
        If scores.ContainsKey(ip) Then
            If newScore > scores(ip).score Then
                scores(ip).score = newScore
                scores(ip).timestamp = DateTime.UtcNow
            End If
        Else
            scores(ip) = New ScoreEntry With {
            .score = newScore,
            .timestamp = DateTime.UtcNow
        }
        End If
        Await SaveScoresAsync()
    End Sub
    Shared Function GetRankByIp(ip As String) As Integer
        ' Sort scores in descending order by score value
        Dim rankedList = scores.OrderByDescending(Function(pair) pair.Value.score).ToList()

        ' Find the index of the specified IP
        For i As Integer = 0 To rankedList.Count - 1
            If rankedList(i).Key = ip Then
                Return i + 1 ' Rank is 1-based
            End If
        Next

        Return 0 ' IP not found / not ranked
    End Function
    Shared Function GetTopScore() As Integer
        If scores Is Nothing Then Return 0
        Return scores.
        OrderByDescending(Function(pair) pair.Value.score).
        Select(Function(pair) pair.Value.score).
        FirstOrDefault()
    End Function

    Shared Function GetTopScoreByMonth(year As Integer, month As Integer) As Integer
        If scores Is Nothing Then Return 0
        Return scores.
        Where(Function(pair) pair.Value.timestamp.Year = year AndAlso pair.Value.timestamp.Month = month).
        OrderByDescending(Function(pair) pair.Value.score).
        Select(Function(pair) pair.Value.score).
        FirstOrDefault()
    End Function


End Class
