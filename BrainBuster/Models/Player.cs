namespace BrainBuster.Models;

// Der Spieler mit Stats und allem drum und dran
public class Player
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = ""; // simple hash reicht für prototyp
    public int TotalScore { get; set; } = 0;
    public int GamesPlayed { get; set; } = 0;
    public int QuestionsCorrect { get; set; } = 0;
    public int QuestionsTotal { get; set; } = 0;
    public int CurrentStreak { get; set; } = 0; // aktuelle Richtig-Serie
    public int BestStreak { get; set; } = 0; // beste Serie ever
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastPlayed { get; set; } = DateTime.Now;

    // Trefferquote berechnen - für Stats
    public double GetAccuracy()
    {
        if (QuestionsTotal == 0) return 0;
        return Math.Round((double)QuestionsCorrect / QuestionsTotal * 100, 1);
    }

    // Durchschnittspunkte pro Spiel
    public double GetAverageScore()
    {
        if (GamesPlayed == 0) return 0;
        return Math.Round((double)TotalScore / GamesPlayed, 1);
    }

    // Simplen Hash erstellen - NICHT für echte Produktion verwenden lol
    public static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password + "brainbuster_salt");
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    // Passwort checken
    public bool CheckPassword(string password)
    {
        return PasswordHash == HashPassword(password);
    }
}
