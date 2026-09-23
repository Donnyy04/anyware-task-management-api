using AnywareTaskManagement.Application.DTOs.Auth;
using AnywareTaskManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnywareTaskManagement.API.Controllers;

[ApiController, Route("api/admin/users"), Authorize(Roles = "Admin")]
public sealed class UsersController(IUserService users) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(RegisterRequest request, CancellationToken ct) => StatusCode(StatusCodes.Status201Created, await users.CreateUserAsync(request, ct));
    [HttpGet] public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await users.GetAllUsersAsync(ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await users.DeleteUserAsync(id, ct); return NoContent(); }
}
