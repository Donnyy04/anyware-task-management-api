using AnywareTaskManagement.Application.DTOs.Auth;
using AnywareTaskManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnywareTaskManagement.API.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous] public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) => Ok(await auth.RegisterAsync(request, ct));
    [HttpPost("login"), AllowAnonymous] public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) => Ok(await auth.LoginAsync(request, ct));
    [HttpPost("refresh"), AllowAnonymous] public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct) => Ok(await auth.RefreshTokenAsync(request.RefreshToken, ct));
    [HttpGet("me"), Authorize] public async Task<IActionResult> Me(CancellationToken ct) => Ok(await auth.GetCurrentUserAsync(ct));
    public sealed class RefreshRequest { public string RefreshToken { get; set; } = string.Empty; }
}
