using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IUserSessionService
{
    Task<List<UserSession>> GetActiveSessionsAsync(string userId);
    Task<UserSession> RegisterSessionAsync(string userId, string deviceName, string ipAddress, string authType);
    Task MarkSessionInactiveAsync(string sessionId);
    Task UpdateLastSeenAsync(string sessionId);
}