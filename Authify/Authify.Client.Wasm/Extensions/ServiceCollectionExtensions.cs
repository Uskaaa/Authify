using System.Text;
using Authify.Application.Data;
using Authify.Client.Wasm.Services;
using Authify.Core.Interfaces;
using Authify.UI.Server.Extensions;
using Authify.UI.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Authify.Client.Wasm.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthifyWasmUI(IServiceCollection services,
        Action<HttpClient> configureClient)
    {
        services.AddAuthifyUI();
        
        services.AddScoped<IAuthRefreshService, AuthRefreshService>();
        services.AddScoped<ITokenStore, TokenStore>();
        
        // Handler registrieren
        services.AddScoped<AuthenticatedHttpClientHandler>();
        
        // HttpClient registrieren und Handler einfügen
        services.AddHttpClient<IAuthifyDataService, WasmDataService>(client =>
            {
                configureClient(client);
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();
        
        return services;
    }
}