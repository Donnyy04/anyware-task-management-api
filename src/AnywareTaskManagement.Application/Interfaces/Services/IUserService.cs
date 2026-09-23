using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AnywareTaskManagement.Application.DTOs.Auth;
using AnywareTaskManagement.Application.DTOs.Users;

namespace AnywareTaskManagement.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserResponse> CreateUserAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetAllUsersAsync(
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
