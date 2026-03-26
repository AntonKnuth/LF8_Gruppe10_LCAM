using BrainBuster;

// === BRAIN BUSTER - Einstiegspunkt ===

// Hilfe anzeigen wenn "h" als Parameter
if (args.Length > 0 && (args[0] == "h" || args[0] == "-h" || args[0] == "--help"))
{
    ConsoleGame.ShowHelp();
    return;
}

// Datenbank und GameManager initialisieren
var db = new Database();
var api = new QuizApi();
var gameManager = new GameManager(db, api);

Console.WriteLine("🧠 Brain Buster wird geladen...\n");

// Konsolenmodus wenn "console" als Parameter
if (args.Length > 0 && args[0].ToLower() == "console")
{
    var consoleGame = new ConsoleGame(db, gameManager);
    await consoleGame.Run();
    return;
}

// Webserver starten (Standard)
Console.WriteLine("Starte im Web-Modus...");
Console.WriteLine("Für Konsolenmodus: dotnet run -- console");
Console.WriteLine("Für Hilfe: dotnet run -- h\n");

var server = new WebServer(db, gameManager);

// Ctrl+C abfangen für sauberes Beenden
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nServer wird beendet...");
    server.Stop();
};

try
{
    await server.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Server Fehler: {ex.Message}");
    
    // Häufiger Fehler: Port bereits belegt oder keine Admin-Rechte
    if (ex.Message.Contains("Access") || ex.Message.Contains("denied"))
    {
        Console.WriteLine("\nTipp: Unter Windows muss der Port ggf. erst freigeschaltet werden:");
        Console.WriteLine("  netsh http add urlacl url=http://localhost:8080/ user=Everyone");
    }
}
