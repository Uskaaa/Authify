using Authify.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Data;

public interface IAuthifyDbContext
{
    DbSet<UserTwoFactor> UserTwoFactors { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<UserExportRequest> UserExportRequests { get; }
    DbSet<UserDeactivationRequest> UserDeactivationRequests { get; }
    DbSet<UserDeletionRequest> UserDeletionRequests { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}