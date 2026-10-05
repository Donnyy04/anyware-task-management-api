using AnywareTaskManagement.Domain.Enums;
using AnywareTaskManagement.Infrastructure.Data;
using AnywareTaskManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

const string apiProject = "src/AnywareTaskManagement.API/AnywareTaskManagement.API.csproj";
if (!File.Exists(apiProject))
{
    Console.Error.WriteLine("Run this utility from the repository root.");
    return 1;
}
var userSecretsId = XDocument.Load(apiProject).Descendants("UserSecretsId").FirstOrDefault()?.Value;
if (string.IsNullOrWhiteSpace(userSecretsId))
{
    Console.Error.WriteLine("Initialize user secrets for the API project first with dotnet user-secrets init.");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(userSecretsId, optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("ConnectionStrings:DefaultConnection is missing. Set it in the API project's user secrets first.");
    return 1;
}

var email = configuration["SeedAdmin:Email"] ?? "admin@example.com";
Console.WriteLine($"This will reset the existing administrator password for {email} in the configured database.");
Console.Write("New password (at least 8 characters; input hidden): ");
var password = ReadPassword();
Console.Write("Confirm new password: ");
var confirmation = ReadPassword();
if (password.Length < 8)
{
    Console.Error.WriteLine("Password must contain at least 8 characters.");
    return 1;
}
if (!string.Equals(password, confirmation, StringComparison.Ordinal))
{
    Console.Error.WriteLine("The passwords do not match.");
    return 1;
}

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(connectionString)
    .Options;
await using var db = new ApplicationDbContext(options);
var admin = await db.Users.SingleOrDefaultAsync(user => user.Email == email);
if (admin is null || admin.Role != UserRole.Admin)
{
    Console.Error.WriteLine("The configured administrator account was not found. No data was changed.");
    return 1;
}

admin.ChangePassword(new PasswordHasher().HashPassword(password));
await db.SaveChangesAsync();
Console.WriteLine("Administrator password updated. The new password was not saved to a file.");
return 0;

static string ReadPassword()
{
    var value = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return value.ToString();
        }
        if (key.Key == ConsoleKey.Backspace)
        {
            if (value.Length > 0) value.Length--;
        }
        else if (!char.IsControl(key.KeyChar))
        {
            value.Append(key.KeyChar);
        }
    }
}

public partial class Program;
