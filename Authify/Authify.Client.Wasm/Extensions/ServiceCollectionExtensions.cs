using Authify.Client.Wasm.Services;
using Authify.Core.Interfaces;
using Authify.UI.Extensions;
using Authify.UI.Services;
using Microsoft.Extensions.DependencyInjection;


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