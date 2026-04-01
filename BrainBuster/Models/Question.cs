namespace BrainBuster.Models;

// Eine Quiz-Frage mit allen Infos die wir brauchen
public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public List<string> WrongAnswers { get; set; } = new();
    public int CategoryId { get; set; }
    public string Difficulty { get; set; } = "medium"; // easy, medium, hard
    public bool IsFromApi { get; set; } = false; // true wenn von OpenTDB

    // Gibt alle Antworten gemischt zurück - nice für die Anzeige
    public List<string> GetShuffledAnswers()
    {
        var allAnswers = new List<string> { CorrectAnswer };
        allAnswers.AddRange(WrongAnswers);

        // Fisher-Yates Shuffle - der klassiker
        var random = new Random();
        for (int i = allAnswers.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (allAnswers[i], allAnswers[j]) = (allAnswers[j], allAnswers[i]);
        }

        return allAnswers;
    }

    // Check ob die Antwort richtig ist (case insensitive)
    public bool IsCorrect(string answer)
    {
        return CorrectAnswer.Equals(answer, StringComparison.OrdinalIgnoreCase);
    }
}