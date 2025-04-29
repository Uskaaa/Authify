using System.Net;
using System.Net.Mail;
using Authify.Core.Interfaces;
using Authify.Infrastructure.Data;
using Authify.Infrastructure.Identity;
using Authify.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Authify.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyAuthInfrastructure(this IServiceCollection services, Action<InfrastructureOptions> configureOptions)
    {
        var options = new InfrastructureOptions();
        configureOptions(options);

        services.AddDbContext<MyAuthDbContext>(db =>
            db.UseSqlServer(options.ConnectionString)); // Oder SQLite/MySQL, je nachdem
        
        services.AddIdentity<ApplicationUser, IdentityRole>(identityOptions =>
            {
                identityOptions.SignIn.RequireConfirmedEmail = true;
                identityOptions.Password.RequireDigit = true;
                identityOptions.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<MyAuthDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserService, IdentityService>();
        services.AddScoped<IEmailSender, EmailSender>();

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