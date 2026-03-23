using Authify.Core.Models.Ldap;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Data;

/// <summary>
/// Erweitert ITeamDbContext um LDAP-spezifische Tabellen.
/// Host-Apps mit LDAP implementieren: IAuthifyDbContext, ITeamDbContext, ILdapDbContext
/// </summary>
public interface ILdapDbContext
{
    DbSet<LdapConfiguration> LdapConfigurations { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
