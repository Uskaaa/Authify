using Authify.Application.Services;
using Authify.Core.Features;
using Authify.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Authify.Application.Extensions;

public static class LdapServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle LDAP-Services.
    /// Muss nach <see cref="ServiceCollectionExtensions.AddAuthifyApplication{TDbContext,TUser}"/>
    /// und <see cref="TeamServiceCollectionExtensions.AddAuthifyTeams{TDbContext,TUser}"/> aufgerufen werden.
    /// TDbContext muss zusätzlich <see cref="Data.ILdapDbContext"/> implementieren.
    /// </summary>
    public static IServiceCollection AddAuthifyLdap<TDbContext, TUser>(this IServiceCollection services)
        where TDbContext : DbContext, Data.IAuthifyDbContext, Data.ITeamDbContext, Data.ILdapDbContext
        where TUser : Data.ApplicationUser, new()
    {
        services.AddScoped<Data.ILdapDbContext>(provider => provider.GetRequiredService<TDbContext>());

        // Überschreibt den NullLdapService-Fallback
        services.AddScoped<ILdapService, LdapService<TUser>>();

        services.AddSingleton(new LdapFeatureOptions { IsEnabled = true });

        return services;
    }
}
