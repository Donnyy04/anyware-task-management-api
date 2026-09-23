using AnywareTaskManagement.Application.DTOs.Auth;
using AnywareTaskManagement.Application.DTOs.Tasks;
using AnywareTaskManagement.Application.DTOs.Users;
using AnywareTaskManagement.Application.Interfaces.Infrastructure;
using AnywareTaskManagement.Application.Interfaces.Repositories;
using AnywareTaskManagement.Application.Interfaces.Services;
using AnywareTaskManagement.Domain.Entities;
using AnywareTaskManagement.Domain.Enums;
using DomainTaskStatus = AnywareTaskManagement.Domain.Enums.TaskStatus;

namespace AnywareTaskManagement.Application.Services;

public sealed class AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens, ICurrentUserService current) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest r, CancellationToken ct = default) { ValidateRegistration(r); if (await users.ExistsByEmailAsync(r.Email.Trim(), ct)) throw new InvalidOperationException("Email is already registered."); var u = new User(r.Name.Trim(), r.Email.Trim().ToLowerInvariant(), hasher.HashPassword(r.Password)); await users.AddAsync(u, ct); return Issue(u); }
    public async Task<AuthResponse> LoginAsync(LoginRequest r, CancellationToken ct = default) { var u = await users.GetByEmailAsync(r.Email.Trim().ToLowerInvariant(), ct); if (u is null || !hasher.VerifyPassword(r.Password, u.PasswordHash)) throw new UnauthorizedAccessException("Invalid email or password."); return Issue(u); }
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default) { var id = tokens.ValidateRefreshToken(refreshToken); if (id is null) throw new UnauthorizedAccessException("Invalid or expired refresh token."); var u = await users.GetByIdAsync(id.Value, ct) ?? throw new UnauthorizedAccessException("User no longer exists."); return Issue(u); }
    public async Task<UserResponse> GetCurrentUserAsync(CancellationToken ct = default) { var id = current.UserId ?? throw new UnauthorizedAccessException(); return Map(await users.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("User not found.")); }
    private AuthResponse Issue(User u) => new() { AccessToken = tokens.GenerateAccessToken(u), RefreshToken = tokens.GenerateRefreshToken(u.Id), AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15) };
    internal static void ValidateRegistration(RegisterRequest r) { if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > 100) throw new ArgumentException("Name is required and must be at most 100 characters."); if (string.IsNullOrWhiteSpace(r.Email) || !System.Net.Mail.MailAddress.TryCreate(r.Email.Trim(), out _)) throw new ArgumentException("A valid email is required."); if (string.IsNullOrWhiteSpace(r.Password) || r.Password.Length < 8) throw new ArgumentException("Password must contain at least 8 characters."); }
    internal static UserResponse Map(User u) => new() { Id = u.Id, Name = u.Name, Email = u.Email, Role = u.Role, CreatedAt = u.CreatedAt };
}

public sealed class UserService(IUserRepository users, IPasswordHasher hasher) : IUserService
{
    public async Task<UserResponse> CreateUserAsync(RegisterRequest r, CancellationToken ct = default) { AuthService.ValidateRegistration(r); var email = r.Email.Trim().ToLowerInvariant(); if (await users.ExistsByEmailAsync(email, ct)) throw new InvalidOperationException("Email is already registered."); var u = new User(r.Name.Trim(), email, hasher.HashPassword(r.Password)); await users.AddAsync(u, ct); return AuthService.Map(u); }
    public async Task<IReadOnlyList<UserResponse>> GetAllUsersAsync(CancellationToken ct = default) => (await users.GetAllAsync(ct)).Select(AuthService.Map).ToArray();
    public async Task DeleteUserAsync(Guid userId, CancellationToken ct = default) { var u = await users.GetByIdAsync(userId, ct) ?? throw new KeyNotFoundException("User not found."); if (u.Role == UserRole.Admin) throw new InvalidOperationException("Admin accounts cannot be deleted through this endpoint."); u.MarkAsDeleted(); await users.UpdateAsync(u, ct); }
}

public sealed class TaskService(ITaskRepository tasks, ICurrentUserService current, ICacheService cache, IBackgroundTaskQueue queue) : ITaskService
{
    private const string CachePrefix = "task:";
    public async Task<TaskResponse> CreateAsync(CreateTaskRequest r, CancellationToken ct = default) { var userId = current.UserId ?? throw new UnauthorizedAccessException(); if (string.IsNullOrWhiteSpace(r.Title) || r.Title.Trim().Length > 200) throw new ArgumentException("Title is required and must be at most 200 characters."); if (!Enum.IsDefined(r.Priority)) throw new ArgumentException("Invalid task priority."); if (await tasks.ExistsWithTitleOnDateAsync(userId, r.Title.Trim(), DateTime.UtcNow, ct)) throw new InvalidOperationException("A task with this title already exists today."); var t = new TaskItem(r.Title.Trim(), r.Description?.Trim() ?? "", r.Priority, userId); await tasks.AddAsync(t, ct); await queue.QueueAsync(t.Id, ct); return Map(t); }
    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken ct = default) { var userId = current.UserId ?? throw new UnauthorizedAccessException(); var cached = await cache.GetAsync<TaskResponse>(CachePrefix + id, ct); if (cached is not null) { if (cached.UserId != userId) throw new KeyNotFoundException("Task not found."); return cached; } var t = await tasks.GetByIdAsync(id, ct); if (t is null || t.UserId != userId) throw new KeyNotFoundException("Task not found."); var response = Map(t); await cache.SetAsync(CachePrefix + id, response, TimeSpan.FromMinutes(10), ct); return response; }
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default) { var userId = current.UserId ?? throw new UnauthorizedAccessException(); return (await tasks.GetByUserIdAsync(userId, ct)).Select(Map).ToArray(); }
    public async Task<TaskResponse> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest r, CancellationToken ct = default) { var userId = current.UserId ?? throw new UnauthorizedAccessException(); if (!Enum.IsDefined(r.Status)) throw new ArgumentException("Invalid task status."); var t = await tasks.GetByIdAsync(id, ct); if (t is null || t.UserId != userId) throw new KeyNotFoundException("Task not found."); t.UpdateStatus(r.Status); await tasks.UpdateAsync(t, ct); await cache.RemoveAsync(CachePrefix + id, ct); return Map(t); }
    private static TaskResponse Map(TaskItem t) => new() { Id=t.Id, Title=t.Title, Description=t.Description, Status=t.Status, Priority=t.Priority, CreatedAt=t.CreatedAt, UserId=t.UserId };
}
