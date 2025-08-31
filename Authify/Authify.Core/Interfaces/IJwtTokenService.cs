using System.Security.Claims;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Authify.Core.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(IdentityUser user, IEnumerable<Claim>? additionalClaims = null);
    RefreshToken GenerateRefreshToken(string userId, string deviceName, string ipAddress, bool rememberMe);
}