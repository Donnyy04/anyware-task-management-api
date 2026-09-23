using AnywareTaskManagement.Application.DTOs.Tasks;
using AnywareTaskManagement.Application.Interfaces.Infrastructure;
using AnywareTaskManagement.Application.Interfaces.Repositories;
using AnywareTaskManagement.Application.Services;
using AnywareTaskManagement.Domain.Entities;
using AnywareTaskManagement.Domain.Enums;
using DomainTaskStatus = AnywareTaskManagement.Domain.Enums.TaskStatus;
using Xunit;

namespace AnywareTaskManagement.Tests;

public sealed class TaskServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsDuplicateTitleForSameUserAndDay()
    {
        var fixture = new Fixture { DuplicateExists = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateAsync(new CreateTaskRequest { Title = "Repeat", Priority = TaskPriority.High }));
        Assert.Empty(fixture.Queue.Queued);
    }

    [Fact]
    public async Task CreateAsync_SavesAndQueuesTask()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.CreateAsync(new CreateTaskRequest { Title = "Ship feature", Priority = TaskPriority.High });
        Assert.Equal(fixture.UserId, result.UserId);
        Assert.Single(fixture.Repository.Items);
        Assert.Equal(result.Id, Assert.Single(fixture.Queue.Queued));
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotExposeAnotherUsersTask()
    {
        var fixture = new Fixture();
        var task = new TaskItem("Private", "", TaskPriority.Low, Guid.NewGuid());
        fixture.Repository.Items.Add(task);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => fixture.Service.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidatesCachedTask()
    {
        var fixture = new Fixture();
        var task = new TaskItem("Cached", "", TaskPriority.Medium, fixture.UserId);
        fixture.Repository.Items.Add(task);
        fixture.Cache.Values["task:" + task.Id] = new TaskResponse { Id = task.Id, UserId = fixture.UserId };
        var updated = await fixture.Service.UpdateStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = DomainTaskStatus.Done });
        Assert.Equal(DomainTaskStatus.Done, updated.Status);
        Assert.Contains("task:" + task.Id, fixture.Cache.Removed);
    }

    private sealed class Fixture
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public bool DuplicateExists { get; init; }
        public FakeTaskRepository Repository { get; } = new();
        public FakeCache Cache { get; } = new();
        public FakeQueue Queue { get; } = new();
        public TaskService Service { get { Repository.DuplicateExists = DuplicateExists; return new(Repository, new CurrentUser(UserId), Cache, Queue); } }
    }

    private sealed class CurrentUser(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public bool IsAuthenticated => true;
        public bool IsAdmin => false;
    }

    private sealed class FakeQueue : IBackgroundTaskQueue
    {
        public List<Guid> Queued { get; } = [];
        public ValueTask QueueAsync(Guid taskId, CancellationToken cancellationToken = default) { Queued.Add(taskId); return ValueTask.CompletedTask; }
        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken) => ValueTask.FromException<Guid>(new NotSupportedException());
    }

    private sealed class FakeCache : ICacheService
    {
        public Dictionary<string, object> Values { get; } = [];
        public List<string> Removed { get; } = [];
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult(Values.TryGetValue(key, out var value) && value is T typed ? typed : default);
        public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) { Values[key] = value!; return Task.CompletedTask; }
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) { Removed.Add(key); Values.Remove(key); return Task.CompletedTask; }
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        public List<TaskItem> Items { get; } = [];
        public bool DuplicateExists { get; set; }
        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TaskItem>>(Items.Where(x => x.UserId == userId).ToArray());
        public Task<bool> ExistsWithTitleOnDateAsync(Guid userId, string title, DateTime date, CancellationToken cancellationToken = default) => Task.FromResult(DuplicateExists || Items.Any(x => x.UserId == userId && x.Title == title && x.CreatedAt.Date == date.Date));
        public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default) { Items.Add(task); return Task.CompletedTask; }
        public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
