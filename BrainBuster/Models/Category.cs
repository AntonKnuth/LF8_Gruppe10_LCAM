namespace BrainBusterV2;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ApiId { get; set; }

    public static List<Category> Defaults() => new()
    {
        new() { Id = 1, Name = "Allgemeinwissen", ApiId = 9 },
        new() { Id = 2, Name = "Film", ApiId = 11 },
        new() { Id = 3, Name = "Musik", ApiId = 12 },
        new() { Id = 4, Name = "Videospiele", ApiId = 15 },
        new() { Id = 5, Name = "Computer", ApiId = 18 },
        new() { Id = 6, Name = "Sport", ApiId = 21 },
        new() { Id = 7, Name = "Geschichte", ApiId = 23 },
        new() { Id = 8, Name = "Geographie", ApiId = 22 }
    };
}