using Authify.Client.Wasm.Interfaces;
using Authify.Client.Wasm.Services;
using Authify.UI.Extensions;
using Authify.UI.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;


namespace Authify.Client.Wasm.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthifyWasmUI(this IServiceCollection services,
        Action<HttpClient> configureClient)
    {
        services.AddAuthifyUI();
        
        services.AddHttpClient<IAuthRefreshService, AuthRefreshService>(client => 
        {
            configureClient(client);
        });
        services.AddScoped<ITokenStore, TokenStore>();
        
        // Handler registrieren
        services.AddScoped<AuthenticatedHttpClientHandler>();
        
        services.AddAuthorizationCore();
        services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
        
        // HttpClient registrieren und Handler einfügen
        services.AddHttpClient<IAuthifyDataService, WasmDataService>(client =>
            {
                configureClient(client);
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();
        return services;
    }
}