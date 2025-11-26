using Authify.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Data;

public interface IAuthifyDbContext
{
    DbSet<UserTwoFactor> UserTwoFactors { get; set; }
    DbSet<UserProfile> UserProfiles { get; set; }
    DbSet<UserSession> UserSessions { get; set; }
    DbSet<UserExportRequest> UserExportRequests { get; set; }
    DbSet<UserDeactivationRequest> UserDeactivationRequests { get; set; }
    DbSet<UserDeletionRequest> UserDeletionRequests { get; set; }
    DbSet<RefreshToken> RefreshTokens { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}