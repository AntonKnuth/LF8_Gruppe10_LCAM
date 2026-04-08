namespace BrainBusterV2.Tests;

/// <summary>Unit-Tests für die Kernlogik.</summary>
public class UnitTests
{
    [Fact]
    public void CalcPoints_CorrectFastAnswer_ReturnsMaxPoints()
    {
        var session = new GameSession { Streak = 3 };

        // Schnelle Antwort (<5s) mit Streak >= 3 → 100 + 50 + 20 = 170
        var points = session.CalcPoints(correct: true, seconds: 3.0);

        Assert.Equal(170, points);
    }

    [Fact]
    public void CalcPoints_WrongAnswer_ReturnsZero()
    {
        var session = new GameSession();

        var points = session.CalcPoints(correct: false, seconds: 2.0);

        Assert.Equal(0, points);
    }

    [Fact]
    public void Question_IsCorrect_CaseInsensitive()
    {
        var question = new Question
        {
            Text = "Hauptstadt von Deutschland?",
            CorrectAnswer = "Berlin",
            WrongAnswers = new List<string> { "Hamburg", "München", "Köln" }
        };

        Assert.True(question.IsCorrect("berlin"));
        Assert.True(question.IsCorrect("BERLIN"));
        Assert.False(question.IsCorrect("Hamburg"));
    }
}

/// <summary>Integrationstest: Datenbank-Registrierung und Login über eine echte SQLite-DB.</summary>
public class DatabaseIntegrationTests : IDisposable
{
    private readonly Database _db;
    private readonly string _dbPath;

    public DatabaseIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"brainbuster_test_{Guid.NewGuid():N}.db");
        _db = new Database(_dbPath);
    }

    [Fact]
    public void Register_And_Login_Roundtrip()
    {
        // Registrierung
        var player = _db.Register("testuser", "geheim123");
        Assert.NotNull(player);
        Assert.Equal("testuser", player!.Username);

        // Login mit korrektem Passwort
        var loggedIn = _db.Login("testuser", "geheim123");
        Assert.NotNull(loggedIn);
        Assert.Equal(player.Id, loggedIn!.Id);

        // Login mit falschem Passwort
        var failed = _db.Login("testuser", "falsch");
        Assert.Null(failed);
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }
}
