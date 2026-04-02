using BrainBuster.Models;

namespace BrainBuster;


//Code
// Konsolen-Version des Spiels
public class ConsoleGame
{
    private readonly Database _db;
    private readonly GameManager _gameManager;
    private Player? _currentPlayer;

    public ConsoleGame(Database db, GameManager gameManager)
    {
        _db = db;
        _gameManager = gameManager;
    }

    // Hilfeanzeige
    public static void ShowHelp()
    {
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║                    🧠 BRAIN BUSTER - HILFE                    ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  STEUERUNG:                                                   ║
║  ─────────                                                    ║
║  1-4      Antwort auswählen                                   ║
║  h        Diese Hilfe anzeigen                                ║
║  q        Spiel beenden                                       ║
║                                                               ║
║  SPIELMODI:                                                   ║
║  ──────────                                                   ║
║  Solo     Spiele alleine gegen die Zeit                       ║
║  Versus   Spiele gegen einen Freund (abwechselnd)             ║
║                                                               ║
║  PUNKTE:                                                      ║
║  ───────                                                      ║
║  Easy     100 Basispunkte                                     ║
║  Medium   200 Basispunkte                                     ║
║  Hard     300 Basispunkte                                     ║
║                                                               ║
║  BONI:                                                        ║
║  ──────                                                       ║
║  Zeitbonus    Schnelle Antwort = mehr Punkte                  ║
║  Streak       Serie richtiger Antworten = Multiplikator       ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
");
    }

    // Hauptmenü starten
    public async Task Run()
    {
        Console.Clear();
        PrintHeader();

        while (true)
        {
            Console.WriteLine("\n=== HAUPTMENÜ ===");
            Console.WriteLine("1. Neues Spiel starten (Solo)");
            Console.WriteLine("2. Versus Modus (2 Spieler)");
            Console.WriteLine("3. Rangliste anzeigen");
            Console.WriteLine("4. Einloggen / Registrieren");
            Console.WriteLine("5. Hilfe");
            Console.WriteLine("0. Beenden");

            if (_currentPlayer != null)
            {
                Console.WriteLine($"\n[Eingeloggt als: {_currentPlayer.Username}]");
            }

            Console.Write("\nAuswahl: ");
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await PlaySolo();
                    break;
                case "2":
                    await PlayVersus();
                    break;
                case "3":
                    ShowLeaderboard();
                    break;
                case "4":
                    HandleLogin();
                    break;
                case "5":
                case "h":
                    ShowHelp();
                    break;
                case "0":
                case "q":
                    Console.WriteLine("\nDanke fürs Spielen! Bis bald! 👋");
                    return;
                default:
                    Console.WriteLine("Ungültige Auswahl!");
                    break;
            }
        }
    }

    // Header ausgeben
    private void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
    ╔═══════════════════════════════════════════════════════╗
    ║   ____             _         ____            _         ║
    ║  | __ ) _ __ __ _ (_)_ __   | __ ) _   _ ___| |_ ___ _ __ ║
    ║  |  _ \| '__/ _` || | '_ \  |  _ \| | | / __| __/ _ \ '__| ║
    ║  | |_) | | | (_| || | | | | | |_) | |_| \__ \ ||  __/ |   ║
    ║  |____/|_|  \__,_|/ |_| |_| |____/ \__,_|___/\__\___|_|   ║
    ║                |__/                                       ║
    ║                                                           ║
    ║              🧠 Das ultimative Quiz-Spiel! 🧠             ║
    ╚═══════════════════════════════════════════════════════╝
");
        Console.ResetColor();
    }

    // Kategorie auswählen
    private int SelectCategory()
    {
        Console.WriteLine("\n=== KATEGORIE WÄHLEN ===");
        var categories = _gameManager.GetCategories();

        Console.WriteLine("0. Alle Kategorien (gemischt)");
        for (int i = 0; i < categories.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {categories[i].Name}");
        }

        Console.Write("\nAuswahl: ");
        if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 0 && choice <= categories.Count)
        {
            return choice == 0 ? 0 : categories[choice - 1].Id;
        }
        return 0;
    }

    // Schwierigkeit auswählen
    private string SelectDifficulty()
    {
        Console.WriteLine("\n=== SCHWIERIGKEIT ===");
        Console.WriteLine("1. Easy (100 Punkte)");
        Console.WriteLine("2. Medium (200 Punkte)");
        Console.WriteLine("3. Hard (300 Punkte)");

        Console.Write("\nAuswahl [2]: ");
        var input = Console.ReadLine()?.Trim();

        return input switch
        {
            "1" => "easy",
            "3" => "hard",
            _ => "medium"
        };
    }

    // Solo Spiel
    private async Task PlaySolo()
    {
        var categoryId = SelectCategory();
        var difficulty = SelectDifficulty();

        Console.Write("\nWie viele Fragen? [10]: ");
        var countInput = Console.ReadLine()?.Trim();
        int questionCount = int.TryParse(countInput, out int c) && c > 0 ? c : 10;

        var playerName = _currentPlayer?.Username ?? "Gast";
        if (_currentPlayer == null)
        {
            Console.Write("Dein Name: ");
            playerName = Console.ReadLine()?.Trim() ?? "Gast";
            if (string.IsNullOrEmpty(playerName)) playerName = "Gast";
        }

        Console.WriteLine("\nLade Fragen...");
        var session = await _gameManager.StartGame(_currentPlayer?.Id, playerName, categoryId, difficulty, questionCount);

        if (session.Questions.Count == 0)
        {
            Console.WriteLine("Keine Fragen gefunden! Bitte später nochmal versuchen.");
            return;
        }

        Console.WriteLine($"\n🎮 Spiel startet mit {session.Questions.Count} Fragen!\n");
        Console.WriteLine("Drücke ENTER um zu beginnen...");
        Console.ReadLine();

        // Fragen durchgehen
        while (!session.IsFinished)
        {
            var question = session.GetCurrentQuestion();
            if (question == null) break;

            Console.Clear();
            Console.WriteLine($"═══════════════════════════════════════════════════════");
            Console.WriteLine($"  Frage {session.CurrentQuestionIndex + 1} von {session.Questions.Count}");
            Console.WriteLine($"  Punkte: {session.Score} | Streak: {session.CurrentStreak}🔥");
            Console.WriteLine($"═══════════════════════════════════════════════════════\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {question.Text}\n");
            Console.ResetColor();

            var answers = question.GetShuffledAnswers();
            for (int i = 0; i < answers.Count; i++)
            {
                Console.WriteLine($"  [{i + 1}] {answers[i]}");
            }

            Console.Write("\nDeine Antwort (1-4): ");
            var startTime = DateTime.Now;
            var input = Console.ReadLine()?.Trim();

            // Abbruch?
            if (input?.ToLower() == "q")
            {
                Console.WriteLine("\nSpiel abgebrochen!");
                break;
            }

            // Antwort verarbeiten
            if (int.TryParse(input, out int answerIndex) && answerIndex >= 1 && answerIndex <= answers.Count)
            {
                var selectedAnswer = answers[answerIndex - 1];
                var (isCorrect, points, correctAnswer) = _gameManager.AnswerQuestion(session.SessionId, selectedAnswer);

                if (isCorrect)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✓ RICHTIG! +{points} Punkte");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n✗ FALSCH! Die richtige Antwort war: {correctAnswer}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("\nUngültige Eingabe - Frage wird als falsch gewertet.");
                _gameManager.AnswerQuestion(session.SessionId, "");
            }

            Console.WriteLine("\nWeiter mit ENTER...");
            Console.ReadLine();
        }

        // Ergebnis anzeigen
        ShowGameResult(session);
    }

    // Versus Modus (2 Spieler)
    private async Task PlayVersus()
    {
        Console.Write("\nName Spieler 1: ");
        var player1Name = Console.ReadLine()?.Trim() ?? "Spieler 1";
        if (string.IsNullOrEmpty(player1Name)) player1Name = "Spieler 1";

        Console.Write("Name Spieler 2: ");
        var player2Name = Console.ReadLine()?.Trim() ?? "Spieler 2";
        if (string.IsNullOrEmpty(player2Name)) player2Name = "Spieler 2";

        var categoryId = SelectCategory();
        var difficulty = SelectDifficulty();

        Console.Write("\nWie viele Fragen pro Spieler? [5]: ");
        var countInput = Console.ReadLine()?.Trim();
        int questionsPerPlayer = int.TryParse(countInput, out int c) && c > 0 ? c : 5;

        Console.WriteLine("\nLade Fragen...");

        // Beide Spieler bekommen die gleichen Fragen
        var session1 = await _gameManager.StartGame(null, player1Name, categoryId, difficulty, questionsPerPlayer);
        var session2 = await _gameManager.StartGame(null, player2Name, categoryId, difficulty, questionsPerPlayer);

        // Gleiche Fragen für beide
        session2.Questions = new List<Question>(session1.Questions);

        if (session1.Questions.Count == 0)
        {
            Console.WriteLine("Keine Fragen gefunden!");
            return;
        }

        // Spieler 1
        Console.Clear();
        Console.WriteLine($"\n🎮 {player1Name} ist dran!\n");
        Console.WriteLine("Drücke ENTER wenn bereit...");
        Console.ReadLine();
        await PlayRound(session1);

        // Spieler 2
        Console.Clear();
        Console.WriteLine($"\n🎮 {player2Name} ist dran!\n");
        Console.WriteLine("Drücke ENTER wenn bereit...");
        Console.ReadLine();
        await PlayRound(session2);

        // Ergebnis
        Console.Clear();
        Console.WriteLine("\n═══════════════════════════════════════════════════");
        Console.WriteLine("              🏆 VERSUS ERGEBNIS 🏆");
        Console.WriteLine("═══════════════════════════════════════════════════\n");

        Console.WriteLine($"  {player1Name}: {session1.Score} Punkte ({session1.CorrectAnswers}/{session1.Questions.Count} richtig)");
        Console.WriteLine($"  {player2Name}: {session2.Score} Punkte ({session2.CorrectAnswers}/{session2.Questions.Count} richtig)\n");

        if (session1.Score > session2.Score)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  🎉 {player1Name} GEWINNT! 🎉");
        }
        else if (session2.Score > session1.Score)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  🎉 {player2Name} GEWINNT! 🎉");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  🤝 UNENTSCHIEDEN! 🤝");
        }
        Console.ResetColor();

        Console.WriteLine("\n═══════════════════════════════════════════════════");
        ShowLeaderboard();
    }

    // Eine Runde spielen (für Versus)
    private async Task PlayRound(GameSession session)
    {
        while (!session.IsFinished)
        {
            var question = session.GetCurrentQuestion();
            if (question == null) break;

            Console.Clear();
            Console.WriteLine($"  Frage {session.CurrentQuestionIndex + 1} von {session.Questions.Count}");
            Console.WriteLine($"  Punkte: {session.Score}\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {question.Text}\n");
            Console.ResetColor();

            var answers = question.GetShuffledAnswers();
            for (int i = 0; i < answers.Count; i++)
            {
                Console.WriteLine($"  [{i + 1}] {answers[i]}");
            }

            Console.Write("\nAntwort (1-4): ");
            var input = Console.ReadLine()?.Trim();

            if (int.TryParse(input, out int idx) && idx >= 1 && idx <= answers.Count)
            {
                var (isCorrect, points, correct) = _gameManager.AnswerQuestion(session.SessionId, answers[idx - 1]);

                if (isCorrect)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✓ RICHTIG! +{points}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n✗ FALSCH! Richtig war: {correct}");
                }
                Console.ResetColor();
            }
            else
            {
                _gameManager.AnswerQuestion(session.SessionId, "");
                Console.WriteLine("\nUngültig - als falsch gewertet.");
            }

            Thread.Sleep(1500);
        }
    }

    // Spielergebnis anzeigen
    private void ShowGameResult(GameSession session)
    {
        Console.Clear();
        Console.WriteLine("\n═══════════════════════════════════════════════════════");
        Console.WriteLine("                   🎮 SPIEL BEENDET 🎮");
        Console.WriteLine("═══════════════════════════════════════════════════════\n");

        Console.WriteLine($"  Spieler:    {session.PlayerName}");
        Console.WriteLine($"  Punkte:     {session.Score}");
        Console.WriteLine($"  Richtig:    {session.CorrectAnswers} von {session.Questions.Count}");
        Console.WriteLine($"  Genauigkeit: {(session.Questions.Count > 0 ? (session.CorrectAnswers * 100 / session.Questions.Count) : 0)}%");
        Console.WriteLine($"  Beste Serie: {session.CurrentStreak}🔥\n");

        // Bewertung
        var percent = session.Questions.Count > 0 ? session.CorrectAnswers * 100 / session.Questions.Count : 0;
        Console.Write("  Bewertung:  ");
        if (percent >= 90)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🏆 PERFEKT!");
        }
        else if (percent >= 70)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("⭐ Sehr gut!");
        }
        else if (percent >= 50)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("👍 Nicht schlecht!");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("📚 Mehr üben!");
        }
        Console.ResetColor();

        // Neue Achievements zeigen
        if (session.PlayerId.HasValue)
        {
            var newAchievements = _gameManager.CheckAchievements(session.PlayerId.Value);
            if (newAchievements.Count > 0)
            {
                Console.WriteLine("\n  🎉 NEUE ACHIEVEMENTS FREIGESCHALTET:");
                foreach (var a in newAchievements)
                {
                    Console.WriteLine($"     {a.Icon} {a.Name} - {a.Description}");
                }
            }
        }

        Console.WriteLine("\n═══════════════════════════════════════════════════════");
        ShowLeaderboard();
    }

    // Rangliste anzeigen
    private void ShowLeaderboard()
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════");
        Console.WriteLine("                   🏆 RANGLISTE 🏆");
        Console.WriteLine("═══════════════════════════════════════════════════════\n");

        var topPlayers = _gameManager.GetLeaderboard(10);

        if (topPlayers.Count == 0)
        {
            Console.WriteLine("  Noch keine Einträge!");
        }
        else
        {
            Console.WriteLine("  Platz  Spieler              Punkte    Spiele  Quote");
            Console.WriteLine("  ─────  ───────────────────  ────────  ──────  ─────");

            for (int i = 0; i < topPlayers.Count; i++)
            {
                var p = topPlayers[i];
                var medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => $" {i + 1}."
                };

                Console.WriteLine($"  {medal,-5} {p.Username,-20} {p.TotalScore,8}  {p.GamesPlayed,6}  {p.GetAccuracy(),5}%");
            }
        }

        Console.WriteLine("\n═══════════════════════════════════════════════════════");
        Console.WriteLine("\nWeiter mit ENTER...");
        Console.ReadLine();
    }

    // Login / Registrierung
    private void HandleLogin()
    {
        Console.WriteLine("\n=== ACCOUNT ===");
        Console.WriteLine("1. Einloggen");
        Console.WriteLine("2. Registrieren");
        Console.WriteLine("3. Ausloggen");
        Console.WriteLine("0. Zurück");

        Console.Write("\nAuswahl: ");
        var choice = Console.ReadLine()?.Trim();

        switch (choice)
        {
            case "1":
                Login();
                break;
            case "2":
                Register();
                break;
            case "3":
                _currentPlayer = null;
                Console.WriteLine("Ausgeloggt!");
                break;
        }
    }

    private void Login()
    {
        Console.Write("\nUsername: ");
        var username = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Passwort: ");
        var password = ReadPassword();

        var player = _db.Login(username, password);
        if (player != null)
        {
            _currentPlayer = player;
            Console.WriteLine($"\nWillkommen zurück, {player.Username}! 🎉");
            Console.WriteLine($"Du hast {player.TotalScore} Punkte und {player.GamesPlayed} Spiele gespielt.");
        }
        else
        {
            Console.WriteLine("\nLogin fehlgeschlagen! Falscher Username oder Passwort.");
        }
    }

    private void Register()
    {
        Console.Write("\nUsername: ");
        var username = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrEmpty(username) || username.Length < 3)
        {
            Console.WriteLine("Username muss mindestens 3 Zeichen haben!");
            return;
        }

        Console.Write("Passwort: ");
        var password = ReadPassword();

        if (password.Length < 4)
        {
            Console.WriteLine("Passwort muss mindestens 4 Zeichen haben!");
            return;
        }

        var player = _db.RegisterPlayer(username, password);
        if (player != null)
        {
            _currentPlayer = player;
            Console.WriteLine($"\nAccount erstellt! Willkommen, {player.Username}! 🎉");
        }
        else
        {
            Console.WriteLine("\nRegistrierung fehlgeschlagen! Username existiert vielleicht schon.");
        }
    }

    // Passwort ohne Echo lesen
    private string ReadPassword()
    {
        var password = "";
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);
            if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
            {
                password += key.KeyChar;
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[..^1];
                Console.Write("\b \b");
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();
        return password;
    }
}