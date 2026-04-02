using System.Net.Http;
using System.Web;

using BrainBuster.Models;

using Newtonsoft.Json;

namespace BrainBuster;

// API-Client für OpenTDB (Open Trivia Database)
// Docs: https://opentdb.com/api_config.php
public class QuizApi
{
    private readonly HttpClient _client;
    private const string BASE_URL = "https://opentdb.com/api.php";

    public QuizApi()
    {
        _client = new HttpClient();
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    // Fragen von OpenTDB holen
    public async Task<List<Question>> GetQuestions(int amount = 10, int? categoryApiId = null, string difficulty = "medium")
    {
        try
        {
            // URL zusammenbauen
            var url = $"{BASE_URL}?amount={amount}&type=multiple";

            if (categoryApiId.HasValue && categoryApiId > 0)
            {
                url += $"&category={categoryApiId}";
            }

            if (!string.IsNullOrEmpty(difficulty) && difficulty != "any")
            {
                url += $"&difficulty={difficulty}";
            }

            Console.WriteLine($"[API] Hole Fragen von: {url}");

            var response = await _client.GetStringAsync(url);
            var apiResponse = JsonConvert.DeserializeObject<OpenTdbResponse>(response);

            if (apiResponse?.ResponseCode != 0 || apiResponse.Results == null)
            {
                Console.WriteLine($"[API] Fehler: Response Code {apiResponse?.ResponseCode}");
                return new List<Question>();
            }

            // API Fragen in unsere Question-Objekte umwandeln
            var questions = apiResponse.Results.Select(r => new Question
            {
                Text = DecodeHtml(r.Question),
                CorrectAnswer = DecodeHtml(r.CorrectAnswer),
                WrongAnswers = r.IncorrectAnswers.Select(DecodeHtml).ToList(),
                Difficulty = r.Difficulty,
                IsFromApi = true
            }).ToList();

            Console.WriteLine($"[API] {questions.Count} Fragen geladen!");
            return questions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API] Fehler beim Laden: {ex.Message}");
            return new List<Question>();
        }
    }

    // HTML Entities dekodieren (API gibt manchmal &quot; etc. zurück)
    private string DecodeHtml(string text)
    {
        return HttpUtility.HtmlDecode(text);
    }

    // Verfügbare Kategorien von API holen
    public async Task<Dictionary<int, string>> GetCategories()
    {
        try
        {
            var response = await _client.GetStringAsync("https://opentdb.com/api_category.php");
            var catResponse = JsonConvert.DeserializeObject<OpenTdbCategoryResponse>(response);

            return catResponse?.TriviaCategories?
                .ToDictionary(c => c.Id, c => c.Name)
                ?? new Dictionary<int, string>();
        }
        catch
        {
            return new Dictionary<int, string>();
        }
    }
}

// === JSON Klassen für API Response ===

public class OpenTdbResponse
{
    [JsonProperty("response_code")]
    public int ResponseCode { get; set; }

    [JsonProperty("results")]
    public List<OpenTdbQuestion>? Results { get; set; }
}

public class OpenTdbQuestion
{
    [JsonProperty("category")]
    public string Category { get; set; } = "";

    [JsonProperty("type")]
    public string Type { get; set; } = "";

    [JsonProperty("difficulty")]
    public string Difficulty { get; set; } = "";

    [JsonProperty("question")]
    public string Question { get; set; } = "";

    [JsonProperty("correct_answer")]
    public string CorrectAnswer { get; set; } = "";

    [JsonProperty("incorrect_answers")]
    public List<string> IncorrectAnswers { get; set; } = new();
}

public class OpenTdbCategoryResponse
{
    [JsonProperty("trivia_categories")]
    public List<OpenTdbCategory>? TriviaCategories { get; set; }
}

public class OpenTdbCategory
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = "";
}