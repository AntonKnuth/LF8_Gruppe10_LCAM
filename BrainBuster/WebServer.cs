using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using BrainBuster.Models;

namespace BrainBuster;

// Einfacher HTTP Server ohne Framework - nur mit HttpListener
public class WebServer
{
    private readonly HttpListener _listener;
    private readonly Database _db;
    private readonly GameManager _gameManager;
    private readonly string _wwwRoot;
    private bool _isRunning;
    
    // JSON Settings für camelCase (JavaScript Standard)
    private readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    // Eingeloggte Sessions (Token -> PlayerId)
    private readonly Dictionary<string, int> _authTokens = new();

    public WebServer(Database db, GameManager gameManager, int port = 8080)
    {
        _db = db;
        _gameManager = gameManager;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");

        // wwwroot Pfad finden
        _wwwRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        if (!Directory.Exists(_wwwRoot))
        {
            _wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }
    }

    // Server starten
    public async Task Start()
    {
        _listener.Start();
        _isRunning = true;
        Console.WriteLine($"\n🌐 Webserver läuft auf http://localhost:8080");
        Console.WriteLine($"📂 wwwroot: {_wwwRoot}");
        Console.WriteLine("\nDrücke Ctrl+C zum Beenden.\n");

        // Cleanup Task für alte Sessions
        _ = Task.Run(async () =>
        {
            while (_isRunning)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                _gameManager.CleanupOldSessions();
            }
        });

        while (_isRunning)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                // Jede Anfrage in eigenem Task bearbeiten
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (Exception ex) when (_isRunning)
            {
                Console.WriteLine($"[Server] Fehler: {ex.Message}");
            }
        }
    }

    // Server stoppen
    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
    }

    // Anfrage bearbeiten
    private async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = request.Url?.AbsolutePath ?? "/";
            var method = request.HttpMethod;

            Console.WriteLine($"[{method}] {path}");

            // CORS Headers für API
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

            // OPTIONS Request für CORS
            if (method == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            // API Routes
            if (path.StartsWith("/api/"))
            {
                await HandleApiRequest(context, path, method);
                return;
            }

            // Statische Dateien ausliefern
            await ServeStaticFile(context, path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] {ex.Message}");
            await SendError(response, 500, "Interner Serverfehler");
        }
    }

    // API Anfragen verarbeiten
    private async Task HandleApiRequest(HttpListenerContext context, string path, string method)
    {
        var response = context.Response;
        var request = context.Request;

        // Body lesen falls POST/PUT
        string body = "";
        if (request.HasEntityBody)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            body = await reader.ReadToEndAsync();
        }

        try
        {
            object? result = null;

            // Auth Routes
            if (path == "/api/auth/register" && method == "POST")
            {
                result = HandleRegister(body);
            }
            else if (path == "/api/auth/login" && method == "POST")
            {
                result = HandleLogin(body);
            }
            else if (path == "/api/auth/me" && method == "GET")
            {
                result = HandleGetMe(request);
            }
            // Game Routes
            else if (path == "/api/game/start" && method == "POST")
            {
                result = await HandleStartGame(body, request);
            }
            else if (path == "/api/game/answer" && method == "POST")
            {
                result = HandleAnswer(body);
            }
            else if (path.StartsWith("/api/game/session/") && method == "GET")
            {
                result = HandleGetSession(path);
            }
            // Categories
            else if (path == "/api/categories" && method == "GET")
            {
                result = _gameManager.GetCategories();
            }
            // Leaderboard
            else if (path == "/api/leaderboard" && method == "GET")
            {
                result = _gameManager.GetLeaderboard(20);
            }
            // Achievements
            else if (path == "/api/achievements" && method == "GET")
            {
                result = Achievement.GetAllAchievements();
            }
            else if (path.StartsWith("/api/achievements/player/") && method == "GET")
            {
                result = HandleGetPlayerAchievements(path);
            }
            // Admin - Fragen
            else if (path == "/api/admin/questions" && method == "GET")
            {
                result = _db.GetAllQuestions();
            }
            else if (path == "/api/admin/questions" && method == "POST")
            {
                result = HandleCreateQuestion(body);
            }
            else if (path.StartsWith("/api/admin/questions/") && method == "PUT")
            {
                result = HandleUpdateQuestion(path, body);
            }
            else if (path.StartsWith("/api/admin/questions/") && method == "DELETE")
            {
                result = HandleDeleteQuestion(path);
            }
            // Admin - Kategorien
            else if (path == "/api/admin/categories" && method == "POST")
            {
                result = HandleCreateCategory(body);
            }

            if (result == null)
            {
                await SendError(response, 404, "Route nicht gefunden");
                return;
            }

            await SendJson(response, result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API Error] {ex.Message}");
            await SendJson(response, new { error = ex.Message }, 400);
        }
    }

    // === AUTH HANDLERS ===

    private object HandleRegister(string body)
    {
        var data = JsonConvert.DeserializeObject<dynamic>(body);
        string username = data?.username ?? "";
        string password = data?.password ?? "";

        if (string.IsNullOrEmpty(username) || username.Length < 3)
            throw new Exception("Username muss min. 3 Zeichen haben");
        if (string.IsNullOrEmpty(password) || password.Length < 4)
            throw new Exception("Passwort muss min. 4 Zeichen haben");

        var player = _db.RegisterPlayer(username, password);
        if (player == null)
            throw new Exception("Username existiert bereits");

        var token = GenerateToken();
        _authTokens[token] = player.Id;

        return new { success = true, token, player = PlayerToDto(player) };
    }

    private object HandleLogin(string body)
    {
        var data = JsonConvert.DeserializeObject<dynamic>(body);
        string username = data?.username ?? "";
        string password = data?.password ?? "";

        var player = _db.Login(username, password);
        if (player == null)
            throw new Exception("Falscher Username oder Passwort");

        var token = GenerateToken();
        _authTokens[token] = player.Id;

        return new { success = true, token, player = PlayerToDto(player) };
    }

    private object? HandleGetMe(HttpListenerRequest request)
    {
        var playerId = GetPlayerIdFromRequest(request);
        if (!playerId.HasValue) return new { loggedIn = false };

        var player = _db.GetPlayerById(playerId.Value);
        if (player == null) return new { loggedIn = false };

        return new { loggedIn = true, player = PlayerToDto(player) };
    }

    // === GAME HANDLERS ===

    private async Task<object> HandleStartGame(string body, HttpListenerRequest request)
    {
        var data = JsonConvert.DeserializeObject<dynamic>(body);
        // Explizit konvertieren weil dynamic manchmal komisch ist
        int categoryId = (int)(data?.categoryId ?? 0);
        string difficulty = (string)(data?.difficulty ?? "medium");
        int questionCount = (int)(data?.questionCount ?? 10);
        string playerName = (string)(data?.playerName ?? "Gast");

        var playerId = GetPlayerIdFromRequest(request);
        if (playerId.HasValue)
        {
            var player = _db.GetPlayerById(playerId.Value);
            if (player != null) playerName = player.Username;
        }

        var session = await _gameManager.StartGame(playerId, playerName, categoryId, difficulty, questionCount);

        return new
        {
            sessionId = session.SessionId,
            totalQuestions = session.Questions.Count,
            currentQuestion = QuestionToDto(session.GetCurrentQuestion(), 1, session.Questions.Count),
            score = session.Score,
            streak = session.CurrentStreak
        };
    }

    private object HandleAnswer(string body)
    {
        var data = JsonConvert.DeserializeObject<dynamic>(body);
        string sessionId = data?.sessionId ?? "";
        string answer = data?.answer ?? "";

        var session = _gameManager.GetSession(sessionId);
        if (session == null)
            throw new Exception("Session nicht gefunden");

        var (isCorrect, points, correctAnswer) = _gameManager.AnswerQuestion(sessionId, answer);

        // Aktualisierte Session holen
        session = _gameManager.GetSession(sessionId)!;

        return new
        {
            isCorrect,
            pointsEarned = points,
            correctAnswer,
            totalScore = session.Score,
            streak = session.CurrentStreak,
            isFinished = session.IsFinished,
            correctAnswers = session.CorrectAnswers,
            currentQuestion = session.IsFinished ? null : QuestionToDto(
                session.GetCurrentQuestion(),
                session.CurrentQuestionIndex + 1,
                session.Questions.Count
            )
        };
    }

    private object? HandleGetSession(string path)
    {
        var sessionId = path.Replace("/api/game/session/", "");
        var session = _gameManager.GetSession(sessionId);
        if (session == null) return null;

        return new
        {
            sessionId = session.SessionId,
            playerName = session.PlayerName,
            score = session.Score,
            correctAnswers = session.CorrectAnswers,
            totalQuestions = session.Questions.Count,
            currentQuestionIndex = session.CurrentQuestionIndex,
            streak = session.CurrentStreak,
            isFinished = session.IsFinished,
            currentQuestion = session.IsFinished ? null : QuestionToDto(
                session.GetCurrentQuestion(),
                session.CurrentQuestionIndex + 1,
                session.Questions.Count
            )
        };
    }

    // === ACHIEVEMENTS HANDLERS ===

    private object HandleGetPlayerAchievements(string path)
    {
        var playerIdStr = path.Replace("/api/achievements/player/", "");
        if (!int.TryParse(playerIdStr, out int playerId))
            throw new Exception("Ungültige Player ID");

        return _gameManager.GetPlayerAchievements(playerId);
    }

    // === ADMIN HANDLERS ===

    private object HandleCreateQuestion(string body)
    {
        var data = JsonConvert.DeserializeObject<dynamic>(body);
        
        var question = new Question
        {
            Text = data?.text ?? "",
            CorrectAnswer = data?.correctAnswer ?? "",
            WrongAnswers = ((Newtonsoft.Json.Linq.JArray?)data?.wrongAnswers)?.ToObject<List<string>>() ?? new List<string>(),
            CategoryId = data?.categoryId ?? 0,
            Difficulty = data?.difficulty ?? "medium"
        };

        if (string.IsNullOrEmpty(question.Text))
            throw new Exception("Fragetext fehlt");
        if (string.IsNullOrEmpty(question.CorrectAnswer))
            throw new Exception("Richtige Antwort fehlt");
        if (question.WrongAnswers.Count < 1)
            throw new Exception("Mindestens eine falsche Antwort nötig");

        _db.InsertQuestion(question);
        return new { success = true, message = "Frage erstellt" };
    }

    private object HandleUpdateQuestion(string path, string body)
    {
        var idStr = path.Replace("/api/admin/questions/", "");
        if (!int.TryParse(idStr, out int id))
            throw new Exception("Ungültige ID");

        var data = JsonConvert.DeserializeObject<dynamic>(body);
        
        var question = new Question
        {
            Id = id,
            Text = data?.text ?? "",
            CorrectAnswer = data?.correctAnswer ?? "",
            WrongAnswers = ((Newtonsoft.Json.Linq.JArray?)data?.wrongAnswers)?.ToObject<List<string>>() ?? new List<string>(),
            CategoryId = data?.categoryId ?? 0,
            Difficulty = data?.difficulty ?? "medium"
        };

        _db.UpdateQuestion(question);
        return new { success = true, message = "Frage aktualisiert" };
    }

    private object HandleDeleteQuestion(string path)
    {
        var idStr = path.Replace("/api/admin/questions/", "");
        if (!int.TryParse(idStr, out int id))
            throw new Exception("Ungültige ID");

        _db.DeleteQuestion(id);
        return new { success = true, message = "Frage gelöscht" };
    }

    private object HandleCreateCategory(string body)
    {
        var data = JsonConvert.DeserializeObject<dynamic>(body);
        
        var category = new Category
        {
            Name = data?.name ?? "",
            Description = data?.description ?? "",
            ApiId = data?.apiId ?? 0
        };

        if (string.IsNullOrEmpty(category.Name))
            throw new Exception("Name fehlt");

        _db.InsertCategory(category);
        return new { success = true, message = "Kategorie erstellt" };
    }

    // === HILFSFUNKTIONEN ===

    // Statische Datei ausliefern
    private async Task ServeStaticFile(HttpListenerContext context, string path)
    {
        var response = context.Response;

        // Default zu index.html
        if (path == "/") path = "/index.html";

        var filePath = Path.Combine(_wwwRoot, path.TrimStart('/'));

        // Sicherheitscheck - kein Path Traversal
        if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(_wwwRoot)))
        {
            await SendError(response, 403, "Zugriff verweigert");
            return;
        }

        if (!File.Exists(filePath))
        {
            // SPA Fallback - bei unbekannten Pfaden index.html liefern
            filePath = Path.Combine(_wwwRoot, "index.html");
            if (!File.Exists(filePath))
            {
                await SendError(response, 404, "Datei nicht gefunden");
                return;
            }
        }

        // Content Type bestimmen
        var contentType = Path.GetExtension(filePath).ToLower() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };

        response.ContentType = contentType;
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        response.ContentLength64 = fileBytes.Length;
        await response.OutputStream.WriteAsync(fileBytes);
        response.Close();
    }

    // JSON Response senden
    private async Task SendJson(HttpListenerResponse response, object data, int statusCode = 200)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        // camelCase für JavaScript Kompatibilität
        var json = JsonConvert.SerializeObject(data, _jsonSettings);
        var bytes = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    // Error Response
    private async Task SendError(HttpListenerResponse response, int statusCode, string message)
    {
        await SendJson(response, new { error = message }, statusCode);
    }

    // Token generieren
    private string GenerateToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    // Player ID aus Request Header holen
    private int? GetPlayerIdFromRequest(HttpListenerRequest request)
    {
        var auth = request.Headers["Authorization"];
        if (string.IsNullOrEmpty(auth)) return null;

        var token = auth.Replace("Bearer ", "").Trim();
        return _authTokens.TryGetValue(token, out int playerId) ? playerId : null;
    }

    // Player zu DTO konvertieren (ohne Passwort)
    private object PlayerToDto(Player p) => new
    {
        id = p.Id,
        username = p.Username,
        totalScore = p.TotalScore,
        gamesPlayed = p.GamesPlayed,
        questionsCorrect = p.QuestionsCorrect,
        questionsTotal = p.QuestionsTotal,
        bestStreak = p.BestStreak,
        accuracy = p.GetAccuracy()
    };

    // Question zu DTO konvertieren (mit gemischten Antworten)
    private object? QuestionToDto(Question? q, int number, int total)
    {
        if (q == null) return null;
        return new
        {
            number,
            total,
            text = q.Text,
            answers = q.GetShuffledAnswers(),
            difficulty = q.Difficulty
        };
    }
}
