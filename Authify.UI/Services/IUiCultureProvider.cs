using Authify.UI.Models;

namespace Authify.UI.Services;

/// <summary>
/// Host extension point for the language switcher in ProfileSettings. Authify has no
/// knowledge of how the host actually switches culture (cookies, URL prefixes, ...);
/// it only asks which cultures are supported and delegates switching to the host.
/// If the host does not register an implementation, the default returns no cultures
/// and the language switcher does not render.
/// </summary>
public interface IUiCultureProvider
{
    IReadOnlyList<UiCulture> GetSupportedCultures();

    Task SetCultureAsync(string cultureCode);
}
