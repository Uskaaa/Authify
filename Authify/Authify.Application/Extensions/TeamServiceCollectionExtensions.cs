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
    /// Registriert alle Team-Account-Services.
    /// Muss nach <see cref="ServiceCollectionExtensions.AddAuthifyApplication{TDbContext,TUser}"/> aufgerufen werden.
    /// TDbContext muss zusätzlich <see cref="Data.ITeamDbContext"/> implementieren.
    /// </summary>
    public static IServiceCollection AddAuthifyTeams<TDbContext, TUser>(this IServiceCollection services)
        where TDbContext : DbContext, Data.IAuthifyDbContext, Data.ITeamDbContext
        where TUser : Data.ApplicationUser, new()
    {
        services.AddScoped<Data.ITeamDbContext>(provider => provider.GetRequiredService<TDbContext>());
        services.AddScoped<ITeamService, TeamService<TUser>>();
        services.AddScoped<ITeamInvitationService, TeamInvitationService<TUser>>();

        // Überschreibt den disabled-Default aus AddAuthifyUI
        services.AddSingleton(new TeamFeatureOptions { IsEnabled = true });

        return services;
    }
}
