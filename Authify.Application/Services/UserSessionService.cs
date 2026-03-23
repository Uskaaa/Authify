using Authify.Application.Data;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class UserSessionService : IUserSessionService
{
    private readonly IAuthifyDbContext _context;

    public UserSessionService(IAuthifyDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserSession>> GetActiveSessionsAsync(string userId)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastSeen)
            .ToListAsync();
    }

    public async Task<UserSession> RegisterSessionAsync(string userId, string deviceName, string ipAddress, string authType)
    {
        var session = new UserSession
        {
            UserId = userId,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            AuthType = authType,
            LastSeen = DateTime.UtcNow
        };

        await _context.UserSessions.AddAsync(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task MarkSessionInactiveAsync(string sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLastSeenAsync(string sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}