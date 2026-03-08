using Authify.UI.Models.Branding;
using Authify.UI.Services;
using Microsoft.Extensions.DependencyInjection;

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
        Action<AuthifyBrandOptions>? configureBranding = null,
        Action<AuthifyRenderOptions>? configureRender = null)
    {
        var brand = new AuthifyBrandOptions();
        configureBranding?.Invoke(brand);
        services.AddSingleton(brand);

        var render = new AuthifyRenderOptions();
        configureRender?.Invoke(render);
        services.AddSingleton(render);
        
        return services;
    }
}