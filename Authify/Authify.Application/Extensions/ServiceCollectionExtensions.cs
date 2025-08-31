using System.Net;
using System.Net.Mail;
using Authify.Application.Data;
using Authify.Application.Services;
using Authify.Core.Extensions;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Authify.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthifyApplication<TDbContext, TUser>(this IServiceCollection services,
        Action<InfrastructureOptions> configureOptions)
        where TDbContext : DbContext, IAuthifyDbContext
        where TUser : IdentityUser, new()
    {
        var options = new InfrastructureOptions();
        configureOptions(options);

        services.AddSingleton(options);

        services.AddScoped<IAuthifyDbContext>(provider => provider.GetRequiredService<TDbContext>());
        
        services.AddScoped<IExternalAuthService, ExternalAuthService<TUser>>();
        services.AddScoped<IEmailSender, EmailService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IUserService, UserService<TUser>>();
        services.AddScoped<ITwoFactorClaimService, TwoFactorClaimService<TUser>>();
        services.AddScoped<IUserProfileService, UserProfileService<TUser>>();
        services.AddScoped<IUserAccountService, UserAccountService<TUser>>();
        services.AddScoped<IUserSessionService, UserSessionService>();
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