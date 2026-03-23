using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IJwtTokenService
{
    Task<string> GenerateTokenAsync(string userId);
    RefreshToken GenerateRefreshToken(string userId, string deviceName, string ipAddress, bool rememberMe);
}