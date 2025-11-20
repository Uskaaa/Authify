using Microsoft.Extensions.DependencyInjection;

namespace Authify.UI.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert UI-spezifische Services für Subify.
    /// Das ISubifyDataService Interface wird von den jeweiligen Client-Projekten registriert.
    /// </summary>
    public static IServiceCollection AddAuthifyUI(this IServiceCollection services)
    {
        
        return services;
    }
}