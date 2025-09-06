using Authify.Application.Data;
using Authify.Application.Extensions;
using Authify.Application.Services;
using Authify.Core.Extensions;
using Authify.Core.Interfaces;
using Authify.UI.Server.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Authify.Client.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthifyServerUI<TDbContext, TUser>(this IServiceCollection services,
        Action<InfrastructureOptions> configureOptions)
        where TDbContext : DbContext, IAuthifyDbContext
        where TUser : IdentityUser, new()
    {
        var options = new InfrastructureOptions();
        configureOptions(options);
        
        services.AddAuthifyApplication<TDbContext, TUser>(configureOptions);
        services.AddAuthifyUI();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = options.GoogleClientId;
                googleOptions.ClientSecret = options.GoogleClientSecret;
            })
            .AddGitHub(githubOptions =>
            {
                githubOptions.ClientId = options.GitHubClientId;
                githubOptions.ClientSecret = options.GitHubClientSecret;
                githubOptions.Scope.Add("user:email");
            })
            .AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = options.FacebookAppId;
                facebookOptions.AppSecret = options.FacebookAppSecret;
                facebookOptions.Fields.Add("email");
            });
            
        services.AddAuthorization();
        services.AddControllers();

        services.AddScoped<IExternalAuthService, ExternalAuthServiceCookie<TUser>>();

        return services;
    }
}