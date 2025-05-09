using System.Net;
using System.Net.Mail;
using System.Text;
using Authify.Application.Data;
using Authify.Application.Services;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Authify.Application.Extensions;

public class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyAuthifyInfrastructure(IServiceCollection services, IConfiguration configuration, Action<InfrastructureOptions> configureOptions)
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
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
                };
            })
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
        services.AddScoped<IExternalAuthService, ExternalAuthService>();
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