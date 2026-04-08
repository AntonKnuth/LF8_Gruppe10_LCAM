namespace BrainBusterV2;

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public List<string> WrongAnswers { get; set; } = new();
    public int CategoryId { get; set; }
    public Difficulty Difficulty { get; set; } = Difficulty.Medium;

    /// <summary>Gibt alle Antworten in zufälliger Reihenfolge zurück.</summary>
    public List<string> GetShuffledAnswers()
    {
        var all = new List<string> { CorrectAnswer };
        all.AddRange(WrongAnswers);
        return all.OrderBy(_ => Random.Shared.Next()).ToList();
    }

    public bool IsCorrect(string answer) =>
        CorrectAnswer.Equals(answer, StringComparison.OrdinalIgnoreCase);
}