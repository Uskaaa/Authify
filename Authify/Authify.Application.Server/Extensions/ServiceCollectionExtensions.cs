using System.Net;
using System.Net.Mail;
using Authify.Application.Data;
using Authify.Application.Services;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Authify.Application.Extensions;

public class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyAuthifyInfrastructure(IServiceCollection services, Action<InfrastructureOptions> configureOptions)
    {
        var options = new InfrastructureOptions();
        configureOptions(options);

        services.AddDbContext<AuthDbContext>(db =>
            db.UseSqlite(options.ConnectionString));
        
        services.AddIdentity<IdentityUser, IdentityRole>(identityOptions =>
            {
                identityOptions.SignIn.RequireConfirmedEmail = true;
                identityOptions.Password.RequireDigit = true;
                identityOptions.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

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
            });
        
        services.AddSingleton(options);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, EmailService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IUserService, UserService>();
        services.AddHttpContextAccessor();
        services.AddDataProtection();

        services.AddSingleton(new SmtpClient
        {
            Host = options.SmtpHost,
            Port = options.SmtpPort,
            EnableSsl = options.EnableSsl,
            Credentials = new NetworkCredential(options.SmtpUsername, options.SmtpPassword)
        });

        return services;
    }
}