using Microsoft.Data.Sqlite;

namespace BrainBusterV2;

public class Database
{
    private readonly string _cs;

    public Database(string path = "brainbuster.db")
    {
        _cs = $"Data Source={path}";
        Init();
    }

    private void Init()
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Players (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT UNIQUE NOT NULL,
                PasswordHash TEXT NOT NULL,
                TotalScore INTEGER DEFAULT 0,
                GamesPlayed INTEGER DEFAULT 0,
                BestStreak INTEGER DEFAULT 0
            )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT UNIQUE NOT NULL,
                ApiId INTEGER DEFAULT 0
            )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Questions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Text TEXT NOT NULL,
                CorrectAnswer TEXT NOT NULL,
                CategoryId INTEGER REFERENCES Categories(Id),
                Difficulty TEXT DEFAULT 'medium'
            )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS WrongAnswers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                QuestionId INTEGER NOT NULL REFERENCES Questions(Id) ON DELETE CASCADE,
                Text TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Scores (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PlayerId INTEGER REFERENCES Players(Id),
                Score INTEGER NOT NULL,
                PlayedAt TEXT DEFAULT CURRENT_TIMESTAMP
            )";
        cmd.ExecuteNonQuery();

        foreach (var c in Category.Defaults())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO Categories (Name, ApiId) VALUES ($n, $a)";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("$n", c.Name);
            cmd.Parameters.AddWithValue("$a", c.ApiId);
            cmd.ExecuteNonQuery();
        }
    }

    public Player? Register(string user, string pw)
    {
        try
        {
            using var conn = new SqliteConnection(_cs);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Players (Username, PasswordHash) VALUES ($u, $p)";
            cmd.Parameters.AddWithValue("$u", user);
            cmd.Parameters.AddWithValue("$p", Player.Hash(pw));
            cmd.ExecuteNonQuery();
            return GetPlayer(user);
        }
        catch { return null; }
    }

    public Player? Login(string user, string pw)
    {
        var p = GetPlayer(user);
        return p?.CheckPassword(pw) == true ? p : null;
    }

    public Player? GetPlayer(string user)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Players WHERE Username = $u";
        cmd.Parameters.AddWithValue("$u", user);
        using var r = cmd.ExecuteReader();
        if (r.Read())
            return new Player
            {
                Id = r.GetInt32(0),
                Username = r.GetString(1),
                PasswordHash = r.GetString(2),
                TotalScore = r.GetInt32(3),
                GamesPlayed = r.GetInt32(4),
                BestStreak = r.GetInt32(5)
            };
        return null;
    }

    public Player? GetPlayerById(int id)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Players WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        if (r.Read())
            return new Player
            {
                Id = r.GetInt32(0),
                Username = r.GetString(1),
                PasswordHash = r.GetString(2),
                TotalScore = r.GetInt32(3),
                GamesPlayed = r.GetInt32(4),
                BestStreak = r.GetInt32(5)
            };
        return null;
    }

    public void UpdatePlayer(int id, int score, int streak)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Players SET 
            TotalScore = TotalScore + $s, 
            GamesPlayed = GamesPlayed + 1,
            BestStreak = MAX(BestStreak, $st) 
            WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$s", score);
        cmd.Parameters.AddWithValue("$st", streak);
        cmd.ExecuteNonQuery();
    }

    public void SaveScore(int? playerId, int score)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Scores (PlayerId, Score) VALUES ($pid, $s)";
        cmd.Parameters.AddWithValue("$pid", playerId.HasValue ? playerId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("$s", score);
        cmd.ExecuteNonQuery();
    }

    public List<(string Name, int Score)> GetLeaderboard(int limit = 10)
    {
        var list = new List<(string, int)>();
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Username, TotalScore FROM Players ORDER BY TotalScore DESC LIMIT $l";
        cmd.Parameters.AddWithValue("$l", limit);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetString(0), r.GetInt32(1)));
        return list;
    }

    public List<Category> GetCategories()
    {
        var list = new List<Category>();
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Categories";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Category { Id = r.GetInt32(0), Name = r.GetString(1), ApiId = r.GetInt32(2) });
        return list;
    }

    public Category? GetCategory(int id)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Categories WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        if (r.Read())
            return new Category { Id = r.GetInt32(0), Name = r.GetString(1), ApiId = r.GetInt32(2) };
        return null;
    }

    public List<Question> GetQuestions(int? catId = null, int limit = 10, Difficulty? difficulty = null)
    {
        var list = new List<Question>();
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        var sql = "SELECT Id, Text, CorrectAnswer, CategoryId, Difficulty FROM Questions";
        var conditions = new List<string>();
        if (catId.HasValue && catId > 0)
        {
            conditions.Add("CategoryId = $c");
            cmd.Parameters.AddWithValue("$c", catId);
        }
        if (difficulty.HasValue)
        {
            conditions.Add("Difficulty = $d");
            cmd.Parameters.AddWithValue("$d", difficulty.Value.ToApiString());
        }
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);
        sql += " ORDER BY RANDOM() LIMIT $l";
        cmd.Parameters.AddWithValue("$l", limit);
        cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Question
            {
                Id = r.GetInt32(0),
                Text = r.GetString(1),
                CorrectAnswer = r.GetString(2),
                CategoryId = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                Difficulty = DifficultyExtensions.ParseDifficulty(r.GetString(4))
            });

        foreach (var q in list)
            q.WrongAnswers = GetWrongAnswers(conn, q.Id);

        return list;
    }

    public List<Question> GetAllQuestions()
    {
        var list = new List<Question>();
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Text, CorrectAnswer, CategoryId, Difficulty FROM Questions ORDER BY Id DESC";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Question
            {
                Id = r.GetInt32(0),
                Text = r.GetString(1),
                CorrectAnswer = r.GetString(2),
                CategoryId = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                Difficulty = DifficultyExtensions.ParseDifficulty(r.GetString(4))
            });

        foreach (var q in list)
            q.WrongAnswers = GetWrongAnswers(conn, q.Id);

        return list;
    }

    public void AddQuestion(Question q)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Questions (Text, CorrectAnswer, CategoryId, Difficulty)
            VALUES ($t, $c, $cat, $d)";
        cmd.Parameters.AddWithValue("$t", q.Text);
        cmd.Parameters.AddWithValue("$c", q.CorrectAnswer);
        cmd.Parameters.AddWithValue("$cat", q.CategoryId);
        cmd.Parameters.AddWithValue("$d", q.Difficulty.ToApiString());
        cmd.ExecuteNonQuery();

        var questionId = (long)new SqliteCommand("SELECT last_insert_rowid()", conn).ExecuteScalar()!;
        foreach (var wrong in q.WrongAnswers)
        {
            var aCmd = conn.CreateCommand();
            aCmd.CommandText = "INSERT INTO WrongAnswers (QuestionId, Text) VALUES ($qid, $t)";
            aCmd.Parameters.AddWithValue("$qid", questionId);
            aCmd.Parameters.AddWithValue("$t", wrong);
            aCmd.ExecuteNonQuery();
        }
    }

    private List<string> GetWrongAnswers(SqliteConnection conn, int questionId)
    {
        var answers = new List<string>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Text FROM WrongAnswers WHERE QuestionId = $qid";
        cmd.Parameters.AddWithValue("$qid", questionId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            answers.Add(r.GetString(0));
        return answers;
    }

    public void UpdateQuestion(int id, Question q)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE Questions SET Text = $t, CorrectAnswer = $c, CategoryId = $cat, Difficulty = $d WHERE Id = $id";
        cmd.Parameters.AddWithValue("$t", q.Text);
        cmd.Parameters.AddWithValue("$c", q.CorrectAnswer);
        cmd.Parameters.AddWithValue("$cat", q.CategoryId);
        cmd.Parameters.AddWithValue("$d", q.Difficulty.ToApiString());
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();

        var delCmd = conn.CreateCommand();
        delCmd.CommandText = "DELETE FROM WrongAnswers WHERE QuestionId = $id";
        delCmd.Parameters.AddWithValue("$id", id);
        delCmd.ExecuteNonQuery();

        foreach (var wrong in q.WrongAnswers)
        {
            var aCmd = conn.CreateCommand();
            aCmd.CommandText = "INSERT INTO WrongAnswers (QuestionId, Text) VALUES ($qid, $t)";
            aCmd.Parameters.AddWithValue("$qid", id);
            aCmd.Parameters.AddWithValue("$t", wrong);
            aCmd.ExecuteNonQuery();
        }
    }

    public void DeleteQuestion(int id)
    {
        using var conn = new SqliteConnection(_cs);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM WrongAnswers WHERE QuestionId = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DELETE FROM Questions WHERE Id = $id";
        cmd.ExecuteNonQuery();
    }
}