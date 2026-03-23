using System.Net;
using System.Net.Mail;
using Authify.Application.Data;
using Authify.Application.Services;
using Authify.Core.Extensions;
using Authify.Core.Features;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Authify.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthifyApplication<TDbContext, TUser>(this IServiceCollection services,
        Action<InfrastructureOptions> configureOptions)
        where TDbContext : DbContext, IAuthifyDbContext
        where TUser : ApplicationUser, new()
    {
        var options = new InfrastructureOptions();
        configureOptions(options);

        services.AddSingleton(options);

        services.AddScoped<IAuthifyDbContext>(provider => provider.GetRequiredService<TDbContext>());
        services.AddScoped<IExternalLoginManagementService, ExternalLoginManagementService<TUser>>();
        services.AddScoped<IEmailSender, EmailService>();
        services.AddScoped<ISmsSender, SmsSender>();
        services.AddScoped<IOtpService<TUser>, OtpService<TUser>>();
        services.AddScoped<IUserService, UserService<TUser>>();
        services.AddScoped<ITwoFactorClaimService, TwoFactorClaimService<TUser>>();
        services.AddScoped<IUserProfileService, UserProfileService<TUser>>();
        services.AddScoped<IUserAccountService, UserAccountService<TUser>>();
        services.AddScoped<IUserSessionService, UserSessionService>();
        services.AddScoped<IUserDataExportService<TUser>, UserDataExportService<TUser>>();
        services.AddScoped<IJwtTokenService, JwtTokenService<TUser>>();
        services.AddScoped<IAuthServiceJwt, JwtAuthService<TUser>>();
        services.AddScoped<IAuthServiceCookie, CookieAuthService<TUser>>();
        services.AddScoped<IPersonalAccessTokenService, PersonalAccessTokenService>();
        services.TryAddScoped<ILdapService, NullLdapService>();
        services.TryAddSingleton(new LdapFeatureOptions { IsEnabled = false });
        services.AddDataProtection();
        services.AddMemoryCache();

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
