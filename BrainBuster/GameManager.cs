using BrainBuster.Models;

namespace BrainBuster;

// Der GameManager kümmert sich um die ganze Spiellogik
public class GameManager
{
    private readonly Database _db;
    private readonly QuizApi _api;
    
    // Aktive Spielsessions (SessionId -> Session)
    private readonly Dictionary<string, GameSession> _sessions = new();

    public GameManager(Database db, QuizApi api)
    {
        _db = db;
        _api = api;
    }

    // Neues Spiel starten
    public async Task<GameSession> StartGame(int? playerId, string playerName, int categoryId = 0, 
        string difficulty = "medium", int questionCount = 10)
    {
        var session = new GameSession
        {
            PlayerId = playerId,
            PlayerName = playerName,
            CategoryId = categoryId,
            Difficulty = difficulty
        };

        // Fragen laden - erst aus DB, dann von API auffüllen
        var questions = new List<Question>();
        
        // Eigene Fragen aus der Datenbank
        var dbQuestions = _db.GetQuestions(categoryId > 0 ? categoryId : null, difficulty, questionCount);
        questions.AddRange(dbQuestions);
        
        // Rest von API holen wenn wir nicht genug haben
        if (questions.Count < questionCount)
        {
            var needed = questionCount - questions.Count;
            var category = categoryId > 0 ? _db.GetCategoryById(categoryId) : null;
            var apiQuestions = await _api.GetQuestions(needed, category?.ApiId, difficulty);
            questions.AddRange(apiQuestions);
        }

        // Falls immer noch nicht genug (API down oder so), Fallback auf alle Difficulties
        if (questions.Count < 3)
        {
            Console.WriteLine("[Game] Nicht genug Fragen, hole alle Difficulties...");
            var category = categoryId > 0 ? _db.GetCategoryById(categoryId) : null;
            var apiQuestions = await _api.GetQuestions(questionCount, category?.ApiId, "any");
            questions.AddRange(apiQuestions);
        }

        // Fragen mischen
        var random = new Random();
        session.Questions = questions.OrderBy(_ => random.Next()).Take(questionCount).ToList();
        session.QuestionStartTime = DateTime.Now;

        // Session speichern
        _sessions[session.SessionId] = session;
        
        Console.WriteLine($"[Game] Neues Spiel gestartet: {session.SessionId} mit {session.Questions.Count} Fragen");
        return session;
    }

    // Session holen
    public GameSession? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    // Antwort verarbeiten
    public (bool isCorrect, int pointsEarned, string correctAnswer) AnswerQuestion(string sessionId, string answer)
    {
        var session = GetSession(sessionId);
        if (session == null || session.IsFinished)
        {
            return (false, 0, "");
        }

        var question = session.GetCurrentQuestion();
        if (question == null)
        {
            return (false, 0, "");
        }

        var timeTaken = session.GetQuestionTime();
        var isCorrect = question.IsCorrect(answer);
        var points = 0;

        if (isCorrect)
        {
            session.CurrentStreak++;
            session.CorrectAnswers++;
            points = session.CalculatePoints(true, timeTaken);
            session.Score += points;
        }
        else
        {
            session.CurrentStreak = 0;
        }

        // Zur nächsten Frage
        session.CurrentQuestionIndex++;
        session.QuestionStartTime = DateTime.Now;

        // Spiel zu Ende?
        if (session.CurrentQuestionIndex >= session.Questions.Count)
        {
            FinishGame(session);
        }

        return (isCorrect, points, question.CorrectAnswer);
    }

    // Spiel beenden
    private void FinishGame(GameSession session)
    {
        session.IsFinished = true;

        // Score in DB speichern
        _db.SaveGameScore(
            session.PlayerId,
            session.PlayerName,
            session.Score,
            session.CorrectAnswers,
            session.Questions.Count,
            session.CategoryId
        );

        // Bei eingeloggtem Spieler: Stats updaten
        if (session.PlayerId.HasValue)
        {
            _db.UpdatePlayerStats(
                session.PlayerId.Value,
                session.Score,
                session.CorrectAnswers,
                session.Questions.Count,
                session.CurrentStreak
            );

            // Achievements checken
            CheckAchievements(session.PlayerId.Value);
        }

        Console.WriteLine($"[Game] Spiel beendet: {session.PlayerName} - {session.Score} Punkte");
    }

    // Achievements für Spieler prüfen und freischalten
    public List<Achievement> CheckAchievements(int playerId)
    {
        var player = _db.GetPlayerById(playerId);
        if (player == null) return new List<Achievement>();

        var unlockedIds = _db.GetPlayerAchievementIds(playerId);
        var allAchievements = Achievement.GetAllAchievements();
        var newlyUnlocked = new List<Achievement>();

        foreach (var achievement in allAchievements)
        {
            // Skip wenn schon freigeschaltet
            if (unlockedIds.Contains(achievement.Id)) continue;

            bool shouldUnlock = achievement.Type switch
            {
                "games_played" => player.GamesPlayed >= achievement.RequiredValue,
                "total_score" => player.TotalScore >= achievement.RequiredValue,
                "streak" => player.BestStreak >= achievement.RequiredValue,
                "accuracy" => player.QuestionsTotal >= 50 && player.GetAccuracy() >= achievement.RequiredValue,
                _ => false
            };

            if (shouldUnlock)
            {
                _db.UnlockAchievement(playerId, achievement.Id);
                newlyUnlocked.Add(achievement);
                Console.WriteLine($"[Achievement] {player.Username} hat '{achievement.Name}' freigeschaltet!");
            }
        }

        return newlyUnlocked;
    }

    // Alle Achievements eines Spielers holen
    public List<Achievement> GetPlayerAchievements(int playerId)
    {
        var unlockedIds = _db.GetPlayerAchievementIds(playerId);
        return Achievement.GetAllAchievements()
            .Where(a => unlockedIds.Contains(a.Id))
            .ToList();
    }

    // Rangliste holen
    public List<Player> GetLeaderboard(int limit = 10)
    {
        return _db.GetTopPlayers(limit);
    }

    // Alle Kategorien holen
    public List<Category> GetCategories()
    {
        return _db.GetAllCategories();
    }

    // Alte Sessions aufräumen (älter als 1 Stunde)
    public void CleanupOldSessions()
    {
        var cutoff = DateTime.Now.AddHours(-1);
        var oldSessions = _sessions
            .Where(s => s.Value.StartTime < cutoff)
            .Select(s => s.Key)
            .ToList();

        foreach (var id in oldSessions)
        {
            _sessions.Remove(id);
        }

        if (oldSessions.Count > 0)
        {
            Console.WriteLine($"[Cleanup] {oldSessions.Count} alte Sessions entfernt");
        }
    }
}
