namespace BrainBuster.Models;

// Quiz-Kategorie (z.B. Sport, Wissenschaft, etc.)
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ApiId { get; set; } = 0; // Die ID von OpenTDB wenn vorhanden

    // Vordefinierte Kategorien von OpenTDB
    public static List<Category> GetDefaultCategories()
    {
        return new List<Category>
        {
            new() { Id = 1, Name = "Allgemeinwissen", Description = "Alles Mögliche", ApiId = 9 },
            new() { Id = 2, Name = "Bücher", Description = "Literatur und Bücher", ApiId = 10 },
            new() { Id = 3, Name = "Film", Description = "Kino und Filme", ApiId = 11 },
            new() { Id = 4, Name = "Musik", Description = "Songs und Künstler", ApiId = 12 },
            new() { Id = 5, Name = "Videospiele", Description = "Gaming stuff", ApiId = 15 },
            new() { Id = 6, Name = "Wissenschaft", Description = "Natur & Wissenschaft", ApiId = 17 },
            new() { Id = 7, Name = "Computer", Description = "IT und Tech", ApiId = 18 },
            new() { Id = 8, Name = "Sport", Description = "Fußball, Tennis, etc.", ApiId = 21 },
            new() { Id = 9, Name = "Geschichte", Description = "Historische Events", ApiId = 23 },
            new() { Id = 10, Name = "Geographie", Description = "Länder und Städte", ApiId = 22 }
        };
    }
}
