using System.Collections.Concurrent;

namespace BrainBusterV2;

public class Game
{
    private readonly Database _db;
    private readonly QuizApi _api;
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    public Game(Database db, QuizApi api)
    {
        _db = db;
        _api = api;
    }

    /// <summary>Startet eine neue Spielsession. Fragen werden aus DB und ggf. API geladen.</summary>
    public async Task<GameSession> Start(int? playerId, string name, int catId = 0, int count = 10, string difficulty = "")
    {
        var session = new GameSession { PlayerId = playerId, PlayerName = name };

        Difficulty? diff = string.IsNullOrEmpty(difficulty) ? null : DifficultyExtensions.ParseDifficulty(difficulty);
        var questions = _db.GetQuestions(catId > 0 ? catId : null, count, diff);

        // Fehlende Fragen aus der externen API nachladen
        if (questions.Count < count)
        {
            var cat = catId > 0 ? _db.GetCategory(catId) : null;
            var api = await _api.GetQuestions(count - questions.Count, cat?.ApiId, diff ?? Difficulty.Medium);
            questions.AddRange(api);
        }

        session.Questions = questions.OrderBy(_ => Guid.NewGuid()).Take(count).ToList();
        session.QuestionStart = DateTime.Now;
        _sessions[session.Id] = session;
        return session;
    }

    public GameSession? Get(string id) => _sessions.TryGetValue(id, out var s) ? s : null;

    public (bool correct, int points, string answer) Answer(string sessionId, string answer)
    {
        var s = Get(sessionId);
        if (s == null || s.Finished) return (false, 0, "");

        var q = s.Current;
        if (q == null) return (false, 0, "");

        var time = (DateTime.Now - s.QuestionStart).TotalSeconds;
        var correct = q.IsCorrect(answer);
        var pts = 0;

        if (correct)
        {
            s.Streak++;
            s.Correct++;
            pts = s.CalcPoints(true, time);
            s.Score += pts;
            if (s.Streak > s.MaxStreak) s.MaxStreak = s.Streak;
        }
        else s.Streak = 0;

        s.CurrentIndex++;
        s.QuestionStart = DateTime.Now;

        if (s.CurrentIndex >= s.Questions.Count)
            Finish(s);

        return (correct, pts, q.CorrectAnswer);
    }

    private void Finish(GameSession s)
    {
        s.Finished = true;
        _db.SaveScore(s.PlayerId, s.Score);
        if (s.PlayerId.HasValue)
            _db.UpdatePlayer(s.PlayerId.Value, s.Score, s.MaxStreak);
    }

    public List<(string, int)> Leaderboard() => _db.GetLeaderboard();
    public List<Category> Categories() => _db.GetCategories();
}