namespace BrainBusterV2;

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

public static class DifficultyExtensions
{
    public static string ToApiString(this Difficulty d) => d switch
    {
        Difficulty.Easy => "easy",
        Difficulty.Medium => "medium",
        Difficulty.Hard => "hard",
        _ => "medium"
    };

    public static Difficulty ParseDifficulty(string? value) => value?.ToLower() switch
    {
        "easy" => Difficulty.Easy,
        "hard" => Difficulty.Hard,
        _ => Difficulty.Medium
    };
}