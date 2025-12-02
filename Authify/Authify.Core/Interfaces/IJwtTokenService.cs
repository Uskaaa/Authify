using System.Security.Claims;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Authify.Core.Interfaces;

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(string userId);
    RefreshToken GenerateRefreshToken(string userId, string deviceName, string ipAddress, bool rememberMe);
}