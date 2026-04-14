namespace Authify.UI.Models.Branding;

/// <summary>
/// Top-level branding configuration for Authify.UI.
/// Register and configure via <c>AddAuthifyUI(opts => { … })</c>
/// in the host project's service registration.
/// </summary>
public sealed class AuthifyBrandOptions
{
    /// <summary>
    /// The application name shown in the mobile top-bar header.
    /// Defaults to "Mycelis".
    /// </summary>
    public string AppName { get; set; } = "Mycelis";

    /// <summary>
    /// Logo displayed in the sidebar (desktop + mobile drawer) and mobile top-bar.
    /// Defaults to the built-in icon-text logo.
    /// Use <see cref="AuthifyLogoOptions.FromIcon"/>, <see cref="AuthifyLogoOptions.FromSvg"/>
    /// or <see cref="AuthifyLogoOptions.FromImage"/> to configure.
    /// </summary>
    public AuthifyLogoOptions Logo { get; set; } = new AuthifyIconTextLogo();

    /// <summary>
    /// Primary color palette and semantic color overrides.
    /// These are injected as CSS custom properties and consumed by both
    /// Tailwind utility classes and scoped page styles.
    /// </summary>
    public AuthifyThemeOptions Theme { get; set; } = new AuthifyThemeOptions();
}
