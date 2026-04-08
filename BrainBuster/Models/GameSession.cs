namespace BrainBusterV2;

public class GameSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int? PlayerId { get; set; }
    public string PlayerName { get; set; } = "Gast";
    public List<Question> Questions { get; set; } = new();
    public int CurrentIndex { get; set; }
    public int Score { get; set; }
    public int Correct { get; set; }
    public int Streak { get; set; }
    public int MaxStreak { get; set; }
    public bool Finished { get; set; }
    public DateTime QuestionStart { get; set; } = DateTime.Now;

    public Question? Current => CurrentIndex < Questions.Count ? Questions[CurrentIndex] : null;

    /// <summary>Berechnet Punkte basierend auf Antwortzeit und aktueller Streak.</summary>
    public int CalcPoints(bool correct, double seconds)
    {
        if (!correct) return 0;
        int pts = 100;
        if (seconds < 5) pts += 50;
        else if (seconds < 10) pts += 25;
        if (Streak >= 3) pts += 20;
        return pts;
    }
}