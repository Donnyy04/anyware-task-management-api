using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AnywareTaskManagement.Domain.Entities;

namespace AnywareTaskManagement.Application.Interfaces.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetPageByUserIdAsync(Guid userId, int skip, int take,
        AnywareTaskManagement.Domain.Enums.TaskStatus? status, string? titleSearch, CancellationToken cancellationToken = default);

    Task<int> CountByUserIdAsync(Guid userId, AnywareTaskManagement.Domain.Enums.TaskStatus? status,
        string? titleSearch, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithTitleOnDateAsync(
        Guid userId,
        string title,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TaskItem task,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        TaskItem task,
        CancellationToken cancellationToken = default);

    Task<bool> TryAdvancePendingAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
