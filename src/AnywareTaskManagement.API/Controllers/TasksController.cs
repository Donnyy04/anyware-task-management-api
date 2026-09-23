using AnywareTaskManagement.Application.DTOs.Tasks;
using AnywareTaskManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnywareTaskManagement.API.Controllers;

[ApiController, Route("api/tasks"), Authorize]
public sealed class TasksController(ITaskService tasks) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken ct) { var task = await tasks.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { id = task.Id }, task); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id, CancellationToken ct) => Ok(await tasks.GetByIdAsync(id, ct));
    [HttpGet] public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await tasks.GetAllAsync(ct));
    [HttpPatch("{id:guid}/status")] public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request, CancellationToken ct) => Ok(await tasks.UpdateStatusAsync(id, request, ct));
}
