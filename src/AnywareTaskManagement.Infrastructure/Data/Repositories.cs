using AnywareTaskManagement.Application.Interfaces.Repositories;
using AnywareTaskManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnywareTaskManagement.Infrastructure.Data;

public sealed class UserRepository(ApplicationDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) => db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) => await db.Users.OrderBy(x => x.CreatedAt).ToListAsync(ct);
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) => db.Users.AnyAsync(x => x.Email == email, ct);
    public async Task AddAsync(User user, CancellationToken ct = default) { db.Users.Add(user); await db.SaveChangesAsync(ct); }
    public async Task UpdateAsync(User user, CancellationToken ct = default) { db.Users.Update(user); await db.SaveChangesAsync(ct); }
}

public sealed class TaskRepository(ApplicationDbContext db) : ITaskRepository
{
    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default) => db.Tasks.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) => await db.Tasks.Where(x => x.UserId == userId).OrderByDescending(x => x.Priority).ThenBy(x => x.CreatedAt).ToListAsync(ct);
    public Task<bool> ExistsWithTitleOnDateAsync(Guid userId, string title, DateTime date, CancellationToken ct = default) => db.Tasks.AnyAsync(x => x.UserId == userId && x.Title == title && x.CreatedAt >= date.Date && x.CreatedAt < date.Date.AddDays(1), ct);
    public async Task AddAsync(TaskItem task, CancellationToken ct = default) { db.Tasks.Add(task); await db.SaveChangesAsync(ct); }
    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default) { db.Tasks.Update(task); await db.SaveChangesAsync(ct); }
}
