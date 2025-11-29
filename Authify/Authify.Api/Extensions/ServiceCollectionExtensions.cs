using System.Text;
using Authify.Application.Data;
using Authify.Application.Extensions;
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
    where TUser : IdentityUser, new()
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
        throw new InvalidOperationException("JWT Konfiguration fehlt! Bitte prüfe appsettings.json oder User Secrets auf 'Jwt:Key', 'Jwt:Issuer' und 'Jwt:Audience'.");
    }
    
    // 3. Key Länge prüfen (SymmetricSecurityKey braucht mind. 16 Zeichen, besser 32)
    if (jwtKey.Length < 32)
    {
        throw new InvalidOperationException("JWT Key ist zu kurz! Er muss mindestens 32 Zeichen lang sein.");
    }

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
                
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                
                // ClockSkew auf 0 setzen ist gut für strikte Ablaufzeiten, 
                // aber Client und Server Uhrzeit müssen synchron sein.
                ClockSkew = TimeSpan.Zero, 
                
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });
    
    services.AddControllers()
        .AddApplicationPart(typeof(Controllers.ExternalAuthController).Assembly);

    services.AddScoped<IExternalAuthService, ExternalAuthServiceJwt<TUser>>();
    
    return services;
}
}