namespace BrainBusterV2;

public class Player
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int TotalScore { get; set; }
    public int GamesPlayed { get; set; }
    public int BestStreak { get; set; }

    // TODO: SHA256 mit festem Salt ist unsicher. Für Produktion bcrypt/Argon2 verwenden.
    public static string Hash(string pw)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(pw + "salt123");
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    public bool CheckPassword(string pw) => PasswordHash == Hash(pw);
}