using Authify.Core.Features;
using Authify.Core.Interfaces;
using Authify.UI.Models.Branding;
using Authify.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Authify.UI.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Authify.UI services.
    /// Optionally configure branding (logo, theme colors, app name) via <paramref name="configureBranding"/>.
    /// If no configuration is provided the built-in Authify defaults are used.
    /// </summary>
    public static IServiceCollection AddAuthifyUI(
        this IServiceCollection services,
        Action<AuthifyBrandOptions>? configureBranding = null)
    {
        var brand = new AuthifyBrandOptions();
        configureBranding?.Invoke(brand);
        services.AddSingleton(brand);

        // Standard: Team-Feature deaktiviert. Wird durch AddAuthifyTeams überschrieben.
        services.TryAddSingleton(new TeamFeatureOptions { IsEnabled = false });

        // Fallback NullTeamDataService – wird durch AddAuthifyWasmTeams / AddAuthifyServerTeams ersetzt.
        services.TryAddScoped<ITeamDataService, NullTeamDataService>();

        return services;
    }
}