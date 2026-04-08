using BrainBusterV2;

var db = new Database();
var api = new QuizApi();
var game = new Game(db, api);

Console.WriteLine("Brain Buster V2");
Console.WriteLine("Starte Web-Modus...\n");

var server = new Server(db, game);

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nBeende...");
    Environment.Exit(0);
};

await server.Start();