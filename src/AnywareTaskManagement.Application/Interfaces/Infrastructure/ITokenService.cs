using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AnywareTaskManagement.Domain.Entities;

namespace AnywareTaskManagement.Application.Interfaces.Infrastructure;

public interface ITokenService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken(Guid userId);

    Guid? ValidateRefreshToken(string token);
}
