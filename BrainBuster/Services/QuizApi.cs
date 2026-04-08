using System.Web;

using Newtonsoft.Json;

namespace BrainBusterV2;

public class QuizApi
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>Lädt Fragen aus der Open Trivia DB und wandelt sie ins interne Format um.</summary>
    public async Task<List<Question>> GetQuestions(int count = 10, int? catApiId = null, Difficulty diff = Difficulty.Medium)
    {
        try
        {
            var url = $"https://opentdb.com/api.php?amount={count}&type=multiple";
            if (catApiId.HasValue && catApiId > 0) url += $"&category={catApiId}";
            url += $"&difficulty={diff.ToApiString()}";

            var json = await _http.GetStringAsync(url);
            var res = JsonConvert.DeserializeObject<ApiResponse>(json);

            if (res?.Results == null) return new();

            return res.Results.Select(r => new Question
            {
                Text = HttpUtility.HtmlDecode(r.Question),
                CorrectAnswer = HttpUtility.HtmlDecode(r.CorrectAnswer),
                WrongAnswers = r.IncorrectAnswers.Select(HttpUtility.HtmlDecode).ToList()!,
                Difficulty = DifficultyExtensions.ParseDifficulty(r.Difficulty)
            }).ToList();
        }
        catch
        {
            return new();
        }
    }

    private class ApiResponse
    {
        [JsonProperty("results")] public List<ApiQuestion>? Results { get; set; }
    }

    private class ApiQuestion
    {
        [JsonProperty("question")] public string Question { get; set; } = "";
        [JsonProperty("correct_answer")] public string CorrectAnswer { get; set; } = "";
        [JsonProperty("incorrect_answers")] public List<string> IncorrectAnswers { get; set; } = new();
        [JsonProperty("difficulty")] public string Difficulty { get; set; } = "";
    }
}