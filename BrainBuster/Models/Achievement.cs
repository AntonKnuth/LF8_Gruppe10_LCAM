namespace BrainBuster.Models;

// Achievement/Erfolg den man freischalten kann
public class Achievement
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "🏆"; // Emoji als Icon - easy
    public int RequiredValue { get; set; } = 0; // z.B. 10 für "10 Spiele gespielt"
    public string Type { get; set; } = ""; // games_played, score, streak, etc.

    // Alle verfügbaren Achievements
    public static List<Achievement> GetAllAchievements()
    {
        return new List<Achievement>
        {
            // Spiele gespielt
            new() { Id = 1, Name = "Anfänger", Description = "Erstes Spiel gespielt", Icon = "🎮", RequiredValue = 1, Type = "games_played" },
            new() { Id = 2, Name = "Stammspieler", Description = "10 Spiele gespielt", Icon = "🎯", RequiredValue = 10, Type = "games_played" },
            new() { Id = 3, Name = "Süchtig", Description = "50 Spiele gespielt", Icon = "🔥", RequiredValue = 50, Type = "games_played" },
            
            // Punkte
            new() { Id = 4, Name = "Punktesammler", Description = "1000 Punkte erreicht", Icon = "⭐", RequiredValue = 1000, Type = "total_score" },
            new() { Id = 5, Name = "Punktekönig", Description = "10000 Punkte erreicht", Icon = "👑", RequiredValue = 10000, Type = "total_score" },
            
            // Streak (Serie)
            new() { Id = 6, Name = "Kleine Serie", Description = "5 Fragen am Stück richtig", Icon = "📈", RequiredValue = 5, Type = "streak" },
            new() { Id = 7, Name = "Unstoppable", Description = "10 Fragen am Stück richtig", Icon = "💪", RequiredValue = 10, Type = "streak" },
            new() { Id = 8, Name = "Perfektionist", Description = "20 Fragen am Stück richtig", Icon = "🏅", RequiredValue = 20, Type = "streak" },
            
            // Genauigkeit
            new() { Id = 9, Name = "Treffsicher", Description = "80% Genauigkeit (min. 50 Fragen)", Icon = "🎯", RequiredValue = 80, Type = "accuracy" },
            new() { Id = 10, Name = "Scharfschütze", Description = "90% Genauigkeit (min. 100 Fragen)", Icon = "🎖️", RequiredValue = 90, Type = "accuracy" },
            
            // Schnelligkeit
            new() { Id = 11, Name = "Blitzmerker", Description = "Frage in unter 3 Sekunden beantwortet", Icon = "⚡", RequiredValue = 3, Type = "fast_answer" },
            new() { Id = 12, Name = "Speedrunner", Description = "10 Fragen in unter 30 Sekunden richtig", Icon = "🚀", RequiredValue = 30, Type = "speed_round" }
        };
    }
}

// Freigeschaltetes Achievement eines Spielers
public class PlayerAchievement
{
    public int PlayerId { get; set; }
    public int AchievementId { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.Now;
}