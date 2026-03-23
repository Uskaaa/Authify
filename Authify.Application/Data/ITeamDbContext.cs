using Authify.Core.Models.Teams;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Data;

/// <summary>
/// Erweitert IAuthifyDbContext um Team-spezifische Tabellen.
/// Muss von Host-Anwendungen implementiert werden, die Team-Features nutzen.
/// </summary>
public interface ITeamDbContext
{
    DbSet<Team> Teams { get; set; }
    DbSet<TeamMember> TeamMembers { get; set; }
    DbSet<TeamInvitation> TeamInvitations { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
