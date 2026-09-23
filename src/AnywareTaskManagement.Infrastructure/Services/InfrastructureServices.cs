using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using AnywareTaskManagement.Application.Interfaces.Infrastructure;
using AnywareTaskManagement.Application.Interfaces.Repositories;
using AnywareTaskManagement.Domain.Entities;
using AnywareTaskManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AnywareTaskManagement.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _hasher = new();
    public string HashPassword(string password) => _hasher.HashPassword(null!, password);
    public bool VerifyPassword(string password, string passwordHash) => _hasher.VerifyHashedPassword(null!, passwordHash, password) != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed;
}

public sealed class TokenService(IConfiguration config) : ITokenService
{
    private string Key => config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
    private string Issuer => config["Jwt:Issuer"] ?? "AnywareTaskManagement";
    public string GenerateAccessToken(User user) => Generate(user, "access", 15);
    public string GenerateRefreshToken(Guid userId) => Generate(new User("Refresh", "refresh@invalid.local", "x"), "refresh", 60 * 24 * 7, userId);
    public Guid? ValidateRefreshToken(string token) { try { var p = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = Issuer, ValidateAudience = true, ValidAudience = Issuer, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30) }, out _); if (p.FindFirst("token_use")?.Value != "refresh" || !Guid.TryParse(p.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)) return null; return id; } catch { return null; } }
    private string Generate(User user, string use, int minutes, Guid? explicitId = null) { var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)); var claims = new[] { new Claim(ClaimTypes.NameIdentifier, (explicitId ?? user.Id).ToString()), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Role.ToString()), new Claim("token_use", use) }; return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(Issuer, Issuer, claims, expires: DateTime.UtcNow.AddMinutes(minutes), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256))); }
}

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public bool IsAdmin => User?.IsInRole(UserRole.Admin.ToString()) == true;
}

public sealed class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) { var v = await _db.StringGetAsync(key); return v.HasValue ? System.Text.Json.JsonSerializer.Deserialize<T>((string)v!) : default; }
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default) => _db.StringSetAsync(key, System.Text.Json.JsonSerializer.Serialize(value), expiration);
    public Task RemoveAsync(string key, CancellationToken ct = default) => _db.KeyDeleteAsync(key);
}

public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();
    public ValueTask QueueAsync(Guid taskId, CancellationToken cancellationToken = default) => _queue.Writer.WriteAsync(taskId, cancellationToken);
    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken) => _queue.Reader.ReadAsync(cancellationToken);
}

public sealed class TaskBackgroundWorker(IBackgroundTaskQueue queue, IServiceScopeFactory scopeFactory, ILogger<TaskBackgroundWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) { while (!stoppingToken.IsCancellationRequested) { try { var id = await queue.DequeueAsync(stoppingToken); await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); using var scope = scopeFactory.CreateScope(); var tasks = scope.ServiceProvider.GetRequiredService<ITaskRepository>(); var cache = scope.ServiceProvider.GetRequiredService<ICacheService>(); var task = await tasks.GetByIdAsync(id, stoppingToken); if (task is not null) { task.UpdateStatus(AnywareTaskManagement.Domain.Enums.TaskStatus.InProgress); await tasks.UpdateAsync(task, stoppingToken); await cache.RemoveAsync("task:" + id, stoppingToken); } } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; } catch (Exception ex) { log.LogError(ex, "Background processing failed for task"); } } }
}

public sealed class AdminSeeder(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<AdminSeeder> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) { using var scope = scopeFactory.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<IUserRepository>(); var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>(); var email = config["SeedAdmin:Email"] ?? "admin@example.com"; if (await users.ExistsByEmailAsync(email, cancellationToken)) return; var password = config["SeedAdmin:Password"] ?? throw new InvalidOperationException("Configure SeedAdmin:Password using user secrets or a secure environment variable before starting the application."); await users.AddAsync(new User("Administrator", email, hasher.HashPassword(password), UserRole.Admin), cancellationToken); log.LogInformation("Seeded default administrator account {Email}", email); }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> log)
{
    public async Task Invoke(HttpContext context) { try { await next(context); } catch (Exception ex) { var (status, title) = ex switch { ArgumentException => (400, "Invalid request"), UnauthorizedAccessException => (401, "Unauthorized"), KeyNotFoundException => (404, "Not found"), InvalidOperationException => (409, "Request conflict"), _ => (500, "Server error") }; if (status == 500) log.LogError(ex, "Unhandled request exception"); context.Response.StatusCode = status; await context.Response.WriteAsJsonAsync(new { status, title, detail = status == 500 ? "An unexpected error occurred." : ex.Message }); } }
}
