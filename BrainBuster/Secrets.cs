using System.Linq;

using Microsoft.Extensions.Configuration;

namespace BrainBusterV2;

// Hallo

/// <summary>Liest Admin-Zugangsdaten aus den .NET User Secrets.</summary>
public static class Secrets
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddUserSecrets(typeof(Secrets).Assembly)
        .Build();

    public static List<AdminUser> Admins => Configuration.GetSection("Admins")
        .GetChildren()
        .Select(section => new AdminUser
        {
            Name = section["Name"] ?? "",
            Password = section["Password"] ?? ""
        })
        .Where(admin => !string.IsNullOrEmpty(admin.Name) && !string.IsNullOrEmpty(admin.Password))
        .ToList();
}

public class AdminUser
{
    public string Name { get; set; } = "";
    public string Password { get; set; } = "";
}