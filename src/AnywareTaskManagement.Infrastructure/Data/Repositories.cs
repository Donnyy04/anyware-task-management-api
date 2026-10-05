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
    public async Task<IReadOnlyList<TaskItem>> GetPageByUserIdAsync(Guid userId, int skip, int take, AnywareTaskManagement.Domain.Enums.TaskStatus? status, string? titleSearch, CancellationToken ct = default)
    {
        var query = FilterTasks(db.Tasks.Where(x => x.UserId == userId), status, titleSearch);
        return await query.OrderByDescending(x => x.Priority).ThenBy(x => x.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }
    public Task<int> CountByUserIdAsync(Guid userId, AnywareTaskManagement.Domain.Enums.TaskStatus? status, string? titleSearch, CancellationToken ct = default) =>
        FilterTasks(db.Tasks.Where(x => x.UserId == userId), status, titleSearch).CountAsync(ct);
    public Task<bool> ExistsWithTitleOnDateAsync(Guid userId, string title, DateTime date, CancellationToken ct = default) => db.Tasks.AnyAsync(x => x.UserId == userId && x.Title == title && x.CreatedAt >= date.Date && x.CreatedAt < date.Date.AddDays(1), ct);
    public async Task AddAsync(TaskItem task, CancellationToken ct = default) { db.Tasks.Add(task); await db.SaveChangesAsync(ct); }
    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default) { db.Tasks.Update(task); await db.SaveChangesAsync(ct); }
    public async Task<bool> TryAdvancePendingAsync(Guid id, CancellationToken ct = default) => await db.Tasks
        .Where(x => x.Id == id && x.Status == AnywareTaskManagement.Domain.Enums.TaskStatus.Pending)
        .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, AnywareTaskManagement.Domain.Enums.TaskStatus.InProgress), ct) == 1;

    private static IQueryable<TaskItem> FilterTasks(IQueryable<TaskItem> query, AnywareTaskManagement.Domain.Enums.TaskStatus? status, string? titleSearch)
    {
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(titleSearch)) query = query.Where(x => x.Title.Contains(titleSearch));
        return query;
    }
}
