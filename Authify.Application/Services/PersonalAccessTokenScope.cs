using Authify.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

/// <summary>
/// PATs carry the owner scope (personal user id or team id) as TenantId, fixed at creation time.
/// These helpers keep that in step with team membership changes so a token neither breaks when
/// its owner's resources move into/out of a team nor keeps team access after its owner leaves.
/// </summary>
internal static class PersonalAccessTokenScope
{
    /// <summary>Moves the user's active tokens from scope <paramref name="fromTenantId"/> to <paramref name="toTenantId"/>.</summary>
    public static async Task MoveAsync(IAuthifyDbContext db, string userId, string fromTenantId, string toTenantId)
    {
        var tokens = await db.PersonalAccessTokens
            .Where(t => t.EndUserId == userId && t.TenantId == fromTenantId && t.RevokedAt == null)
            .ToListAsync();
        foreach (var token in tokens)
            token.TenantId = toTenantId;
        await db.SaveChangesAsync();
    }

    /// <summary>Revokes the active tokens in scope <paramref name="tenantId"/>, optionally only those of one user.</summary>
    public static async Task RevokeAsync(IAuthifyDbContext db, string tenantId, string? userId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var tokens = await db.PersonalAccessTokens
            .Where(t => t.TenantId == tenantId && t.RevokedAt == null && (userId == null || t.EndUserId == userId))
            .ToListAsync();
        foreach (var token in tokens)
            token.RevokedAt = now;
        await db.SaveChangesAsync();
    }
}
