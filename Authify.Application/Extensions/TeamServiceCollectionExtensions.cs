using Authify.Application.Services;
using Authify.Core.Features;
using Authify.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Authify.Application.Extensions;

public static class TeamServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle Team-Account-Services inkl. LDAP (LDAP ist ein integraler Bestandteil von Teams).
    /// Muss nach <see cref="ServiceCollectionExtensions.AddAuthifyApplication{TDbContext,TUser}"/> aufgerufen werden.
    /// TDbContext muss <see cref="Data.ITeamDbContext"/> und <see cref="Data.ILdapDbContext"/> implementieren.
    /// </summary>
    public static IServiceCollection AddAuthifyTeams<TDbContext, TUser>(this IServiceCollection services)
        where TDbContext : DbContext, Data.IAuthifyDbContext, Data.ITeamDbContext, Data.ILdapDbContext
        where TUser : Data.ApplicationUser, new()
    {
        services.AddScoped<Data.ITeamDbContext>(provider => provider.GetRequiredService<TDbContext>());
        services.AddScoped<ITeamService, TeamService<TUser>>();
        services.AddScoped<ITeamInvitationService, TeamInvitationService<TUser>>();

        services.AddSingleton(new TeamFeatureOptions { IsEnabled = true });

        // LDAP ist Teil von Teams – wird automatisch mitregistriert
        services.AddAuthifyLdap<TDbContext, TUser>();

        return services;
    }
}
