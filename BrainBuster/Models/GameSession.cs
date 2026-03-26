namespace BrainBuster.Models;

// Eine laufende Spielsession
public class GameSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public int? PlayerId { get; set; } // null wenn Gast
    public string PlayerName { get; set; } = "Gast";
    public List<Question> Questions { get; set; } = new();
    public int CurrentQuestionIndex { get; set; } = 0;
    public int Score { get; set; } = 0;
    public int CorrectAnswers { get; set; } = 0;
    public int CurrentStreak { get; set; } = 0;
    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime? QuestionStartTime { get; set; }
    public bool IsFinished { get; set; } = false;
    public string GameMode { get; set; } = "solo"; // solo, versus
    public int CategoryId { get; set; } = 0; // 0 = alle Kategorien
    public string Difficulty { get; set; } = "medium";

    // Aktuelle Frage holen
    public Question? GetCurrentQuestion()
    {
        if (CurrentQuestionIndex >= Questions.Count) return null;
        return Questions[CurrentQuestionIndex];
    }

    // Punkte berechnen basierend auf Zeit und Difficulty
    public int CalculatePoints(bool isCorrect, double secondsTaken)
    {
        if (!isCorrect) return 0;

        // Basispunkte je nach Schwierigkeit
        int basePoints = Difficulty switch
        {
            "easy" => 100,
            "medium" => 200,
            "hard" => 300,
            _ => 200
        };

        // Zeitbonus: Je schneller, desto mehr Punkte (max 50% extra)
        double timeBonus = 1.0;
        if (secondsTaken < 5) timeBonus = 1.5;
        else if (secondsTaken < 10) timeBonus = 1.3;
        else if (secondsTaken < 15) timeBonus = 1.1;

        // Streak Bonus: Ab 3er Serie gibt's extra
        double streakBonus = 1.0;
        if (CurrentStreak >= 10) streakBonus = 1.5;
        else if (CurrentStreak >= 5) streakBonus = 1.3;
        else if (CurrentStreak >= 3) streakBonus = 1.1;

        return (int)(basePoints * timeBonus * streakBonus);
    }

    // Zeit für aktuelle Frage
    public double GetQuestionTime()
    {
        if (QuestionStartTime == null) return 0;
        return (DateTime.Now - QuestionStartTime.Value).TotalSeconds;
    }
}
