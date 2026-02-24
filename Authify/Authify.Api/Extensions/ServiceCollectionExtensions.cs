using System.Text;
using Authify.Application.Data;
using Authify.Application.Extensions;
using Authify.Application.Interfaces;
using Authify.Application.Services;
using Authify.Core.Extensions;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Authify.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthifyApiEndpoints<TDbContext, TUser>(this IServiceCollection services,
        IConfiguration configuration,
        Action<InfrastructureOptions> configureOptions)
        where TDbContext : DbContext, IAuthifyDbContext
        where TUser : ApplicationUser, new()
    {
        var options = new InfrastructureOptions();
        configureOptions(options);

        services.AddAuthifyApplication<TDbContext, TUser>(configureOptions);

        // 1. Werte sicher auslesen
        var jwtKey = configuration["Jwt:Key"];
        var jwtIssuer = configuration["Jwt:Issuer"];
        var jwtAudience = configuration["Jwt:Audience"];

        // 2. Prüfen, ob die Config da ist (Verhindert den "Value cannot be null" Absturz)
        if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
        {
            throw new InvalidOperationException(
                "JWT Konfiguration fehlt! Bitte prüfe appsettings.json oder User Secrets auf 'Jwt:Key', 'Jwt:Issuer' und 'Jwt:Audience'.");
        }

        // 3. Key Länge prüfen (SymmetricSecurityKey braucht mind. 16 Zeichen, besser 32)
        if (jwtKey.Length < 32)
        {
            throw new InvalidOperationException("JWT Key ist zu kurz! Er muss mindestens 32 Zeichen lang sein.");
        }

        const string JwtAuthScheme = "JwtAuth";

        var authBuilder =  services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtAuthScheme;
                options.DefaultScheme = JwtAuthScheme;
                options.DefaultChallengeScheme = JwtAuthScheme;
            })
            .AddPolicyScheme(JwtAuthScheme, "Bearer or Cookie", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api") || 
                        context.Request.Path.StartsWithSegments("/auth") ||
                        context.Request.Path.StartsWithSegments("/oidc"))
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }

                    return IdentityConstants.ApplicationScheme;
                };
            })
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
                
                jwt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        if (!string.IsNullOrEmpty(accessToken) && 
                            path.StartsWithSegments("/auth/external-login"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        
        if (!string.IsNullOrEmpty(options.GoogleClientId) && !string.IsNullOrEmpty(options.GoogleClientSecret))
        {
            authBuilder.AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = options.GoogleClientId;
                googleOptions.ClientSecret = options.GoogleClientSecret;

                googleOptions.SignInScheme = IdentityConstants.ExternalScheme;
            });
        }
        
        if (!string.IsNullOrEmpty(options.GitHubClientId) && !string.IsNullOrEmpty(options.GitHubClientSecret))
        {
            authBuilder.AddGitHub(githubOptions =>
            {
                githubOptions.ClientId = options.GitHubClientId;
                githubOptions.ClientSecret = options.GitHubClientSecret;

                githubOptions.SignInScheme = IdentityConstants.ExternalScheme;

                githubOptions.Scope.Add("user:email");
            });
        }

        if (!string.IsNullOrEmpty(options.FacebookAppId) && !string.IsNullOrEmpty(options.FacebookAppSecret))
        {
            authBuilder.AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = options.FacebookAppId;
                facebookOptions.AppSecret = options.FacebookAppSecret;

                facebookOptions.SignInScheme = IdentityConstants.ExternalScheme;
            });
        }

        services.AddControllers()
            .AddApplicationPart(typeof(Controllers.ExternalAuthController).Assembly);

        services.AddScoped<IExternalAuthService, ExternalAuthServiceJwt<TUser>>();

        return services;
    }
}