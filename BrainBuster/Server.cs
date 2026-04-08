using System.Collections.Concurrent;
using System.Net;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BrainBusterV2;

public class Server
{
    private readonly HttpListener _listener = new();
    private readonly Database _db;
    private readonly Game _game;
    private readonly string _webRoot;
    private readonly JsonSerializerSettings _json = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    // Thread-sichere Token-Speicher für parallele Requests
    private readonly ConcurrentDictionary<string, int> _tokens = new();
    private readonly ConcurrentDictionary<string, bool> _adminTokens = new();
    private bool _running;

    public Server(Database db, Game game, int port = 8080)
    {
        _db = db;
        _game = game;
        _listener.Prefixes.Add($"http://+:{port}/");
        _webRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        if (!Directory.Exists(_webRoot))
            _webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    public async Task Start()
    {
        _listener.Start();
        _running = true;
        Console.WriteLine($"\nServer laeuft auf http://localhost:8080");
        Console.WriteLine($"Web-Ordner: {_webRoot}");
        Console.WriteLine("Ctrl+C zum Beenden\n");

        while (_running)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                _ = Task.Run(() => Handle(ctx));
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex) { Console.WriteLine($"Listener-Fehler: {ex.Message}"); }
        }
    }

    private async Task Handle(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        var path = req.Url?.AbsolutePath ?? "/";
        var method = req.HttpMethod;

        res.AddHeader("Access-Control-Allow-Origin", "*");
        res.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        res.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

        if (method == "OPTIONS") { res.StatusCode = 200; res.Close(); return; }

        try
        {
            if (path.StartsWith("/api/"))
            {
                var body = "";
                if (req.HasEntityBody)
                    using (var sr = new StreamReader(req.InputStream))
                        body = await sr.ReadToEndAsync();
                await HandleApi(res, req, path, method, body);
            }
            else
                await ServeFile(res, path);
        }
        catch (Exception ex)
        {
            await SendJson(res, new { error = ex.Message }, 500);
        }
    }

    private async Task HandleApi(HttpListenerResponse res, HttpListenerRequest req, string path, string method, string body)
    {
        object? result = null;

        if (path == "/api/register" && method == "POST")
        {
            var d = JsonConvert.DeserializeObject<dynamic>(body);
            var p = _db.Register((string)d!.username, (string)d.password);
            if (p == null) throw new Exception("Username existiert bereits");
            var token = Guid.NewGuid().ToString("N");
            _tokens[token] = p.Id;
            result = new { token, player = new { p.Id, p.Username, p.TotalScore, p.GamesPlayed } };
        }
        else if (path == "/api/login" && method == "POST")
        {
            var d = JsonConvert.DeserializeObject<dynamic>(body);
            var p = _db.Login((string)d!.username, (string)d.password);
            if (p == null) throw new Exception("Login fehlgeschlagen");
            var token = Guid.NewGuid().ToString("N");
            _tokens[token] = p.Id;
            result = new { token, player = new { p.Id, p.Username, p.TotalScore, p.GamesPlayed } };
        }
        else if (path == "/api/me" && method == "GET")
        {
            var pid = GetPlayerId(req);
            if (pid == null) result = new { loggedIn = false };
            else
            {
                var p = _db.GetPlayerById(pid.Value);
                result = p == null ? new { loggedIn = false }
                    : new { loggedIn = true, player = new { p.Id, p.Username, p.TotalScore, p.GamesPlayed } };
            }
        }
        else if (path == "/api/start" && method == "POST")
        {
            var d = JsonConvert.DeserializeObject<dynamic>(body);
            var pid = GetPlayerId(req);
            var name = pid != null ? _db.GetPlayerById(pid.Value)?.Username ?? "Gast" : (string)(d!.playerName ?? "Gast");
            var cat = (int)(d!.categoryId ?? 0);
            var count = (int)(d.questionCount ?? 10);
            var diff = (string)(d.difficulty ?? "");
            var s = await _game.Start(pid, name, cat, count, diff);
            result = new
            {
                sessionId = s.Id,
                total = s.Questions.Count,
                question = ToQuestion(s.Current, s.CurrentIndex + 1, s.Questions.Count),
                score = s.Score,
                streak = s.Streak
            };
        }
        else if (path == "/api/answer" && method == "POST")
        {
            var d = JsonConvert.DeserializeObject<dynamic>(body);
            var (correct, pts, ans) = _game.Answer((string)d!.sessionId, (string)d.answer);
            var s = _game.Get((string)d.sessionId);
            result = new
            {
                correct,
                points = pts,
                correctAnswer = ans,
                score = s?.Score ?? 0,
                streak = s?.Streak ?? 0,
                finished = s?.Finished ?? true,
                question = s?.Finished != true ? ToQuestion(s?.Current, (s?.CurrentIndex ?? 0) + 1, s?.Questions.Count ?? 0) : null
            };
        }
        else if (path == "/api/categories" && method == "GET")
            result = _game.Categories();
        else if (path == "/api/leaderboard" && method == "GET")
            result = _game.Leaderboard().Select(x => new { name = x.Item1, score = x.Item2 });
        else if (path == "/api/admin/login" && method == "POST")
        {
            var d = JsonConvert.DeserializeObject<dynamic>(body);
            if (Secrets.Admins.Any(a => a.Name == (string)d!.username && a.Password == (string)d.password))
            {
                // Admin-Token generieren und serverseitig speichern
                var adminToken = "admin_" + Guid.NewGuid().ToString("N");
                _adminTokens[adminToken] = true;
                result = new { success = true, token = adminToken };
            }
            else
                throw new Exception("Admin-Login fehlgeschlagen");
        }
        else if (path == "/api/admin/questions" && method == "GET")
        {
            if (!IsAdmin(req)) throw new Exception("Nicht autorisiert");
            result = _db.GetAllQuestions();
        }
        else if (path == "/api/admin/questions" && method == "POST")
        {
            if (!IsAdmin(req)) throw new Exception("Nicht autorisiert");
            var d = JsonConvert.DeserializeObject<dynamic>(body);
            var q = new Question
            {
                Text = (string)d!.text,
                CorrectAnswer = (string)d.correctAnswer,
                WrongAnswers = ((Newtonsoft.Json.Linq.JArray)d.wrongAnswers).ToObject<List<string>>()!,
                CategoryId = (int)(d.categoryId ?? 0),
                Difficulty = DifficultyExtensions.ParseDifficulty((string)(d.difficulty ?? "medium"))
            };
            _db.AddQuestion(q);
            result = new { success = true };
        }
        else if (path.StartsWith("/api/admin/questions/") && method == "PUT")
        {
            if (!IsAdmin(req)) throw new Exception("Nicht autorisiert");
            var id = int.Parse(path.Split('/').Last());
            var d = JsonConvert.DeserializeObject<dynamic>(body);
            var q = new Question
            {
                Text = (string)d!.text,
                CorrectAnswer = (string)d.correctAnswer,
                WrongAnswers = ((Newtonsoft.Json.Linq.JArray)d.wrongAnswers).ToObject<List<string>>()!,
                CategoryId = (int)(d.categoryId ?? 0),
                Difficulty = DifficultyExtensions.ParseDifficulty((string)(d.difficulty ?? "medium"))
            };
            _db.UpdateQuestion(id, q);
            result = new { success = true };
        }
        else if (path.StartsWith("/api/admin/questions/") && method == "DELETE")
        {
            if (!IsAdmin(req)) throw new Exception("Nicht autorisiert");
            var id = int.Parse(path.Split('/').Last());
            _db.DeleteQuestion(id);
            result = new { success = true };
        }

        if (result == null)
            await SendJson(res, new { error = "Not found" }, 404);
        else
            await SendJson(res, result);
    }

    /// <summary>Prüft ob ein gültiger, serverseitig gespeicherter Admin-Token vorliegt.</summary>
    private bool IsAdmin(HttpListenerRequest req)
    {
        var auth = req.Headers["Authorization"]?.Replace("Bearer ", "");
        return !string.IsNullOrEmpty(auth) && _adminTokens.ContainsKey(auth);
    }

    private int? GetPlayerId(HttpListenerRequest req)
    {
        var auth = req.Headers["Authorization"]?.Replace("Bearer ", "");
        if (string.IsNullOrEmpty(auth) || auth.StartsWith("admin_")) return null;
        return _tokens.TryGetValue(auth, out var id) ? id : null;
    }

    private object? ToQuestion(Question? q, int num, int total) => q == null ? null : new
    {
        number = num,
        total,
        text = q.Text,
        answers = q.GetShuffledAnswers()
    };

    private async Task ServeFile(HttpListenerResponse res, string path)
    {
        if (path == "/") path = "/index.html";
        var file = Path.GetFullPath(Path.Combine(_webRoot, path.TrimStart('/')));

        // Path Traversal verhindern: Datei muss innerhalb von wwwroot liegen
        if (!file.StartsWith(Path.GetFullPath(_webRoot)))
        {
            res.StatusCode = 403;
            res.Close();
            return;
        }

        if (!File.Exists(file)) file = Path.Combine(_webRoot, "index.html");
        if (!File.Exists(file)) { res.StatusCode = 404; res.Close(); return; }

        res.ContentType = Path.GetExtension(file) switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            _ => "application/octet-stream"
        };
        var bytes = await File.ReadAllBytesAsync(file);
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }

    private async Task SendJson(HttpListenerResponse res, object data, int code = 200)
    {
        res.StatusCode = code;
        res.ContentType = "application/json; charset=utf-8";
        var json = JsonConvert.SerializeObject(data, _json);
        var bytes = Encoding.UTF8.GetBytes(json);
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }
}