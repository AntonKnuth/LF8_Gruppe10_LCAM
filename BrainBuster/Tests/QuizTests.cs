using Xunit;
using BrainBuster;
using BrainBuster.Models;

namespace BrainBuster.Tests;

// Unit Tests für Brain Buster
// Ausführen mit: dotnet test
public class QuizTests
{
    // === TEST 1: Question Model Tests ===
    [Fact]
    public void Question_IsCorrect_ShouldReturnTrue_WhenAnswerMatches()
    {
        // Arrange - Frage erstellen
        var question = new Question
        {
            Text = "Was ist 2+2?",
            CorrectAnswer = "4",
            WrongAnswers = new List<string> { "3", "5", "6" }
        };

        // Act & Assert - Richtige Antwort prüfen
        Assert.True(question.IsCorrect("4"));
        Assert.True(question.IsCorrect("4")); // Exakt
        Assert.False(question.IsCorrect("3")); // Falsch
        Assert.False(question.IsCorrect("")); // Leer
    }

    [Fact]
    public void RedTest()
    {
        Assert.Equal(1, 0);
    }

    [Fact]
    public void Question_IsCorrect_ShouldBeCaseInsensitive()
    {
        // Case insensitive sollte funktionieren
        var question = new Question
        {
            Text = "Hauptstadt von Deutschland?",
            CorrectAnswer = "Berlin",
            WrongAnswers = new List<string> { "München", "Hamburg", "Köln" }
        };

        Assert.True(question.IsCorrect("berlin")); // lowercase
        Assert.True(question.IsCorrect("BERLIN")); // uppercase
        Assert.True(question.IsCorrect("Berlin")); // normal
    }

    [Fact]
    public void Question_GetShuffledAnswers_ShouldContainAllAnswers()
    {
        var question = new Question
        {
            CorrectAnswer = "Richtig",
            WrongAnswers = new List<string> { "Falsch1", "Falsch2", "Falsch3" }
        };

        var shuffled = question.GetShuffledAnswers();

        // Alle 4 Antworten sollten drin sein
        Assert.Equal(4, shuffled.Count);
        Assert.Contains("Richtig", shuffled);
        Assert.Contains("Falsch1", shuffled);
        Assert.Contains("Falsch2", shuffled);
        Assert.Contains("Falsch3", shuffled);
    }

    // === TEST 2: Player Model Tests ===
    [Fact]
    public void Player_GetAccuracy_ShouldCalculateCorrectly()
    {
        var player = new Player
        {
            QuestionsCorrect = 8,
            QuestionsTotal = 10
        };

        // 8/10 = 80%
        Assert.Equal(80.0, player.GetAccuracy());
    }

    [Fact]
    public void Player_GetAccuracy_ShouldReturnZero_WhenNoQuestions()
    {
        var player = new Player
        {
            QuestionsCorrect = 0,
            QuestionsTotal = 0
        };

        // Keine Division durch 0!
        Assert.Equal(0, player.GetAccuracy());
    }

    [Fact]
    public void Player_HashPassword_ShouldBeConsistent()
    {
        // Gleicher Input = gleicher Hash
        var hash1 = Player.HashPassword("test123");
        var hash2 = Player.HashPassword("test123");
        
        Assert.Equal(hash1, hash2);
        
        // Verschiedene Inputs = verschiedene Hashes
        var hash3 = Player.HashPassword("anderes");
        Assert.NotEqual(hash1, hash3);
    }

    [Fact]
    public void Player_CheckPassword_ShouldWork()
    {
        var player = new Player
        {
            PasswordHash = Player.HashPassword("meinPasswort")
        };

        Assert.True(player.CheckPassword("meinPasswort"));
        Assert.False(player.CheckPassword("falschesPasswort"));
    }

    // === TEST 3: GameSession Tests ===
    [Fact]
    public void GameSession_CalculatePoints_ShouldReturnZero_WhenWrong()
    {
        var session = new GameSession { Difficulty = "medium" };
        
        // Falsche Antwort = 0 Punkte
        var points = session.CalculatePoints(false, 5);
        Assert.Equal(0, points);
    }

    [Fact]
    public void GameSession_CalculatePoints_ShouldGiveBonus_WhenFast()
    {
        var session = new GameSession { Difficulty = "medium", CurrentStreak = 0 };
        
        // Schnelle Antwort (unter 5 Sekunden) = mehr Punkte
        var fastPoints = session.CalculatePoints(true, 3);
        var slowPoints = session.CalculatePoints(true, 20);
        
        // 200 * 1.5 = 300 für schnell
        // 200 * 1.0 = 200 für langsam
        Assert.True(fastPoints > slowPoints);
    }

    [Fact]
    public void GameSession_CalculatePoints_ShouldGiveStreakBonus()
    {
        // Mit Streak
        var sessionWithStreak = new GameSession { Difficulty = "medium", CurrentStreak = 5 };
        var pointsWithStreak = sessionWithStreak.CalculatePoints(true, 15);
        
        // Ohne Streak
        var sessionNoStreak = new GameSession { Difficulty = "medium", CurrentStreak = 0 };
        var pointsNoStreak = sessionNoStreak.CalculatePoints(true, 15);
        
        Assert.True(pointsWithStreak > pointsNoStreak);
    }

    [Fact]
    public void GameSession_GetCurrentQuestion_ShouldReturnNull_WhenFinished()
    {
        var session = new GameSession
        {
            Questions = new List<Question>
            {
                new() { Text = "Frage 1" }
            },
            CurrentQuestionIndex = 1 // Über dem Index
        };

        Assert.Null(session.GetCurrentQuestion());
    }

    // === TEST 4: Category Tests ===
    [Fact]
    public void Category_GetDefaultCategories_ShouldReturnCategories()
    {
        var categories = Category.GetDefaultCategories();
        
        // Sollte mehrere Kategorien haben
        Assert.True(categories.Count >= 5);
        
        // Bestimmte Kategorien sollten existieren
        Assert.Contains(categories, c => c.Name == "Allgemeinwissen");
        Assert.Contains(categories, c => c.Name == "Sport");
    }

    // === TEST 5: Achievement Tests ===
    [Fact]
    public void Achievement_GetAllAchievements_ShouldReturnAchievements()
    {
        var achievements = Achievement.GetAllAchievements();
        
        Assert.True(achievements.Count >= 10);
        
        // Verschiedene Types sollten existieren
        Assert.Contains(achievements, a => a.Type == "games_played");
        Assert.Contains(achievements, a => a.Type == "streak");
        Assert.Contains(achievements, a => a.Type == "total_score");
    }

    [Fact]
    public void Achievement_ShouldHaveUniqueIds()
    {
        var achievements = Achievement.GetAllAchievements();
        var ids = achievements.Select(a => a.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();
        
        // Alle IDs sollten einzigartig sein
        Assert.Equal(ids.Count, uniqueIds.Count);
    }
}
