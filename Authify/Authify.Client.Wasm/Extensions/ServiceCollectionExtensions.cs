using Authify.Client.Wasm.Interfaces;
using Authify.Client.Wasm.Services;
using Authify.UI.Extensions;
using Authify.UI.Models.Branding;
using Authify.UI.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;


namespace Authify.Client.Wasm.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Authify WASM UI services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureClient">Configures the base address and headers for the Authify API HttpClient.</param>
    /// <param name="configureBranding">Optional branding configuration (logo, theme colors, app name).</param>
    public static IServiceCollection AddAuthifyWasmUI(this IServiceCollection services,
        Action<HttpClient> configureClient,
        Action<AuthifyBrandOptions>? configureBranding = null)
    {
        services.AddAuthifyUI(configureBranding);
        
        services.AddHttpClient<IAuthRefreshService, AuthRefreshService>(client => 
        {
            configureClient(client);
        });
        services.AddScoped<ITokenStore, TokenStore>();
        
        // Handler registrieren
        services.AddScoped<AuthenticatedHttpClientHandler>();
        
        services.AddAuthorizationCore();
        services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
        services.AddScoped<TokenRefreshManager>();
        
        // HttpClient registrieren und Handler einfügen
        services.AddHttpClient<IAuthifyDataService, WasmDataService>(client =>
            {
                configureClient(client);
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();
        return services;
    }
}