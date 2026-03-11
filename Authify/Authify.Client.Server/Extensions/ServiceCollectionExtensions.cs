using Authify.Application.Data;
using Authify.Application.Extensions;
using Authify.Application.Interfaces;
using Authify.Application.Services;
using Authify.Client.Server.Services;
using Authify.Core.Extensions;
using Authify.Core.Features;
using Authify.Core.Interfaces;
using Authify.UI.Extensions;
using Authify.UI.Models.Branding;
using Authify.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Authify.Client.Server.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Authify server-side UI and application services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Infrastructure options (DB, OAuth providers, …).</param>
    /// <param name="configureBranding">Optional branding configuration (logo, theme colors, app name).</param>
    public static IServiceCollection AddAuthifyServerUI<TDbContext, TUser>(this IServiceCollection services,
        Action<InfrastructureOptions> configureOptions,
        Action<AuthifyBrandOptions>? configureBranding = null)
        where TDbContext : DbContext, IAuthifyDbContext
        where TUser : ApplicationUser, new()
    {
        var options = new InfrastructureOptions();
        configureOptions(options);
        
        services.AddAuthifyUI(configureBranding);
        
        services.AddAuthifyApplication<TDbContext, TUser>(configureOptions);

        // ensure HttpContextAccessor is available for ServerDataService
        services.AddHttpContextAccessor();

        // Named HttpClient used by ServerDataService to call local cookie-setting endpoints.
        // UseCookies=false is required so that Set-Cookie headers are accessible on the
        // HttpResponseMessage and can be forwarded back to the browser's HTTP response.
        services.AddHttpClient("AuthifyServerLocal")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false
            });

        // register server-side implementation of IAuthifyDataService
        services.AddScoped<IAuthifyDataService, ServerDataService<TUser>>();

        var authBuilder = services.AddAuthentication(authOptions =>
            {
                authOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie();

        if (!string.IsNullOrEmpty(options.GoogleClientId) && !string.IsNullOrEmpty(options.GoogleClientSecret))
        {
            authBuilder.AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = options.GoogleClientId;
                googleOptions.ClientSecret = options.GoogleClientSecret;
            });
        }

        if (!string.IsNullOrEmpty(options.GitHubClientId) && !string.IsNullOrEmpty(options.GitHubClientSecret))
        {
            authBuilder.AddGitHub(githubOptions =>
            {
                githubOptions.ClientId = options.GitHubClientId;
                githubOptions.ClientSecret = options.GitHubClientSecret;
                githubOptions.Scope.Add("user:email");
            });
        }

        if (!string.IsNullOrEmpty(options.FacebookAppId) && !string.IsNullOrEmpty(options.FacebookAppSecret))
        {
            authBuilder.AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = options.FacebookAppId;
                facebookOptions.AppSecret = options.FacebookAppSecret;
                facebookOptions.Fields.Add("email");
            });
        }

        services.AddAuthorization();
        services.AddControllers();

        services.AddScoped<IExternalAuthService, ExternalAuthServiceCookie<TUser>>();

        return services;
    }

    /// <summary>
    /// Aktiviert Team-Account-Features für die Server-seitige Blazor-App.
    /// Muss nach <see cref="AddAuthifyServerUI{TDbContext,TUser}"/> aufgerufen werden.
    /// TDbContext muss zusätzlich <see cref="Application.Data.ITeamDbContext"/> implementieren.
    /// </summary>
    public static IServiceCollection AddAuthifyServerTeams<TDbContext, TUser>(this IServiceCollection services)
        where TDbContext : DbContext, IAuthifyDbContext, Application.Data.ITeamDbContext
        where TUser : ApplicationUser, new()
    {
        services.AddAuthifyTeams<TDbContext, TUser>();
        services.AddScoped<ITeamDataService, ServerTeamDataService>();
        return services;
    }
}