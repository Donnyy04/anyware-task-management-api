using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AnywareTaskManagement.Application.DTOs.Tasks;

namespace AnywareTaskManagement.Application.Interfaces.Services;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(
        CreateTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<TaskResponse> GetByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<TaskResponse> UpdateStatusAsync(
        Guid taskId,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken = default);
}