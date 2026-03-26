using Microsoft.Data.Sqlite;
using BrainBuster.Models;

namespace BrainBuster;

// Datenbank-Helper für alle SQLite Operationen
public class Database
{
    private readonly string _connectionString;

    public Database(string dbPath = "brainbuster.db")
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    // Datenbank und Tabellen erstellen falls nicht vorhanden
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        
        // Spieler Tabelle
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Players (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT UNIQUE NOT NULL,
                PasswordHash TEXT NOT NULL,
                TotalScore INTEGER DEFAULT 0,
                GamesPlayed INTEGER DEFAULT 0,
                QuestionsCorrect INTEGER DEFAULT 0,
                QuestionsTotal INTEGER DEFAULT 0,
                CurrentStreak INTEGER DEFAULT 0,
                BestStreak INTEGER DEFAULT 0,
                CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                LastPlayed TEXT DEFAULT CURRENT_TIMESTAMP
            )";
        command.ExecuteNonQuery();

        // Kategorien Tabelle
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                ApiId INTEGER DEFAULT 0
            )";
        command.ExecuteNonQuery();

        // Fragen Tabelle (für eigene Fragen)
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Questions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Text TEXT NOT NULL,
                CorrectAnswer TEXT NOT NULL,
                WrongAnswers TEXT NOT NULL,
                CategoryId INTEGER,
                Difficulty TEXT DEFAULT 'medium',
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            )";
        command.ExecuteNonQuery();

        // Achievements Tabelle
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS PlayerAchievements (
                PlayerId INTEGER,
                AchievementId INTEGER,
                UnlockedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (PlayerId, AchievementId),
                FOREIGN KEY (PlayerId) REFERENCES Players(Id)
            )";
        command.ExecuteNonQuery();

        // Highscores Tabelle für einzelne Spiele
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS GameScores (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PlayerId INTEGER,
                PlayerName TEXT NOT NULL,
                Score INTEGER NOT NULL,
                CorrectAnswers INTEGER,
                TotalQuestions INTEGER,
                CategoryId INTEGER,
                PlayedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (PlayerId) REFERENCES Players(Id)
            )";
        command.ExecuteNonQuery();

        // Default Kategorien einfügen wenn leer
        command.CommandText = "SELECT COUNT(*) FROM Categories";
        var count = (long)command.ExecuteScalar()!;
        
        if (count == 0)
        {
            foreach (var cat in Category.GetDefaultCategories())
            {
                InsertCategory(cat);
            }
            Console.WriteLine("Default Kategorien wurden erstellt.");
        }
    }

    // === SPIELER FUNKTIONEN ===

    // Neuen Spieler registrieren
    public Player? RegisterPlayer(string username, string password)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Players (Username, PasswordHash)
                VALUES ($username, $password)";
            command.Parameters.AddWithValue("$username", username);
            command.Parameters.AddWithValue("$password", Player.HashPassword(password));
            command.ExecuteNonQuery();

            return GetPlayerByUsername(username);
        }
        catch (SqliteException)
        {
            // Username existiert wahrscheinlich schon
            return null;
        }
    }

    // Spieler per Username holen
    public Player? GetPlayerByUsername(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Players WHERE Username = $username";
        command.Parameters.AddWithValue("$username", username);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return ReadPlayer(reader);
        }
        return null;
    }

    // Spieler per ID holen
    public Player? GetPlayerById(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Players WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return ReadPlayer(reader);
        }
        return null;
    }

    // Login check
    public Player? Login(string username, string password)
    {
        var player = GetPlayerByUsername(username);
        if (player != null && player.CheckPassword(password))
        {
            return player;
        }
        return null;
    }

    // Spieler Stats updaten nach einem Spiel
    public void UpdatePlayerStats(int playerId, int scoreGained, int correct, int total, int streak)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Players SET
                TotalScore = TotalScore + $score,
                GamesPlayed = GamesPlayed + 1,
                QuestionsCorrect = QuestionsCorrect + $correct,
                QuestionsTotal = QuestionsTotal + $total,
                CurrentStreak = $streak,
                BestStreak = MAX(BestStreak, $streak),
                LastPlayed = CURRENT_TIMESTAMP
            WHERE Id = $id";
        command.Parameters.AddWithValue("$id", playerId);
        command.Parameters.AddWithValue("$score", scoreGained);
        command.Parameters.AddWithValue("$correct", correct);
        command.Parameters.AddWithValue("$total", total);
        command.Parameters.AddWithValue("$streak", streak);
        command.ExecuteNonQuery();
    }

    // Hilfsfunktion: Player aus Reader lesen
    private Player ReadPlayer(SqliteDataReader reader)
    {
        return new Player
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            TotalScore = reader.GetInt32(3),
            GamesPlayed = reader.GetInt32(4),
            QuestionsCorrect = reader.GetInt32(5),
            QuestionsTotal = reader.GetInt32(6),
            CurrentStreak = reader.GetInt32(7),
            BestStreak = reader.GetInt32(8)
        };
    }

    // === RANGLISTE FUNKTIONEN ===

    // Top Spieler holen (Leaderboard)
    public List<Player> GetTopPlayers(int limit = 10)
    {
        var players = new List<Player>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT * FROM Players 
            ORDER BY TotalScore DESC 
            LIMIT $limit";
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            players.Add(ReadPlayer(reader));
        }
        return players;
    }

    // Letzte Spiel-Scores holen
    public List<(string PlayerName, int Score, DateTime PlayedAt)> GetRecentScores(int limit = 10)
    {
        var scores = new List<(string, int, DateTime)>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT PlayerName, Score, PlayedAt FROM GameScores 
            ORDER BY PlayedAt DESC 
            LIMIT $limit";
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            scores.Add((
                reader.GetString(0),
                reader.GetInt32(1),
                DateTime.Parse(reader.GetString(2))
            ));
        }
        return scores;
    }

    // Spiel-Score speichern
    public void SaveGameScore(int? playerId, string playerName, int score, int correct, int total, int categoryId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO GameScores (PlayerId, PlayerName, Score, CorrectAnswers, TotalQuestions, CategoryId)
            VALUES ($playerId, $name, $score, $correct, $total, $category)";
        command.Parameters.AddWithValue("$playerId", playerId.HasValue ? playerId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$name", playerName);
        command.Parameters.AddWithValue("$score", score);
        command.Parameters.AddWithValue("$correct", correct);
        command.Parameters.AddWithValue("$total", total);
        command.Parameters.AddWithValue("$category", categoryId);
        command.ExecuteNonQuery();
    }

    // === KATEGORIE FUNKTIONEN ===

    // Alle Kategorien holen
    public List<Category> GetAllCategories()
    {
        var categories = new List<Category>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Categories ORDER BY Name";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categories.Add(new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ApiId = reader.GetInt32(3)
            });
        }
        return categories;
    }

    // Kategorie per ID holen
    public Category? GetCategoryById(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Categories WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ApiId = reader.GetInt32(3)
            };
        }
        return null;
    }

    // Kategorie einfügen
    public void InsertCategory(Category category)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Categories (Name, Description, ApiId)
            VALUES ($name, $desc, $apiId)";
        command.Parameters.AddWithValue("$name", category.Name);
        command.Parameters.AddWithValue("$desc", category.Description);
        command.Parameters.AddWithValue("$apiId", category.ApiId);
        command.ExecuteNonQuery();
    }

    // === FRAGEN FUNKTIONEN ===

    // Eigene Frage speichern
    public void InsertQuestion(Question question)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Questions (Text, CorrectAnswer, WrongAnswers, CategoryId, Difficulty)
            VALUES ($text, $correct, $wrong, $category, $difficulty)";
        command.Parameters.AddWithValue("$text", question.Text);
        command.Parameters.AddWithValue("$correct", question.CorrectAnswer);
        command.Parameters.AddWithValue("$wrong", string.Join("|||", question.WrongAnswers));
        command.Parameters.AddWithValue("$category", question.CategoryId);
        command.Parameters.AddWithValue("$difficulty", question.Difficulty);
        command.ExecuteNonQuery();
    }

    // Eigene Fragen aus DB holen
    public List<Question> GetQuestions(int? categoryId = null, string? difficulty = null, int limit = 10)
    {
        var questions = new List<Question>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        var sql = "SELECT * FROM Questions WHERE 1=1";
        
        if (categoryId.HasValue && categoryId > 0)
        {
            sql += " AND CategoryId = $category";
            command.Parameters.AddWithValue("$category", categoryId.Value);
        }
        if (!string.IsNullOrEmpty(difficulty))
        {
            sql += " AND Difficulty = $difficulty";
            command.Parameters.AddWithValue("$difficulty", difficulty);
        }
        
        sql += " ORDER BY RANDOM() LIMIT $limit";
        command.Parameters.AddWithValue("$limit", limit);
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            questions.Add(new Question
            {
                Id = reader.GetInt32(0),
                Text = reader.GetString(1),
                CorrectAnswer = reader.GetString(2),
                WrongAnswers = reader.GetString(3).Split("|||").ToList(),
                CategoryId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                Difficulty = reader.GetString(5),
                IsFromApi = false
            });
        }
        return questions;
    }

    // Alle eigenen Fragen holen (für Admin)
    public List<Question> GetAllQuestions()
    {
        var questions = new List<Question>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Questions ORDER BY Id DESC";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            questions.Add(new Question
            {
                Id = reader.GetInt32(0),
                Text = reader.GetString(1),
                CorrectAnswer = reader.GetString(2),
                WrongAnswers = reader.GetString(3).Split("|||").ToList(),
                CategoryId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                Difficulty = reader.GetString(5),
                IsFromApi = false
            });
        }
        return questions;
    }

    // Frage aktualisieren
    public void UpdateQuestion(Question question)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Questions SET
                Text = $text,
                CorrectAnswer = $correct,
                WrongAnswers = $wrong,
                CategoryId = $category,
                Difficulty = $difficulty
            WHERE Id = $id";
        command.Parameters.AddWithValue("$id", question.Id);
        command.Parameters.AddWithValue("$text", question.Text);
        command.Parameters.AddWithValue("$correct", question.CorrectAnswer);
        command.Parameters.AddWithValue("$wrong", string.Join("|||", question.WrongAnswers));
        command.Parameters.AddWithValue("$category", question.CategoryId);
        command.Parameters.AddWithValue("$difficulty", question.Difficulty);
        command.ExecuteNonQuery();
    }

    // Frage löschen
    public void DeleteQuestion(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Questions WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    // === ACHIEVEMENTS FUNKTIONEN ===

    // Achievement freischalten
    public void UnlockAchievement(int playerId, int achievementId)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO PlayerAchievements (PlayerId, AchievementId)
                VALUES ($playerId, $achievementId)";
            command.Parameters.AddWithValue("$playerId", playerId);
            command.Parameters.AddWithValue("$achievementId", achievementId);
            command.ExecuteNonQuery();
        }
        catch { /* Ignorieren wenn schon vorhanden */ }
    }

    // Spieler Achievements holen
    public List<int> GetPlayerAchievementIds(int playerId)
    {
        var ids = new List<int>();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AchievementId FROM PlayerAchievements WHERE PlayerId = $playerId";
        command.Parameters.AddWithValue("$playerId", playerId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ids.Add(reader.GetInt32(0));
        }
        return ids;
    }
}
