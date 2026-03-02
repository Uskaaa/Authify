namespace Authify.UI.Models.Branding;

/// <summary>
/// Base class for logo configuration. Use one of the factory methods or concrete types.
/// </summary>
public abstract class AuthifyLogoOptions
{
    /// <summary>
    /// Creates a logo using a FontAwesome icon in a gradient box next to styled text.
    /// </summary>
    public static AuthifyIconTextLogo FromIcon(
        string iconClass,
        string textPrefix,
        string textHighlight,
        string gradientFrom = "auth-from-primary-500",
        string gradientTo = "auth-to-indigo-700")
        => new()
        {
            IconClass = iconClass,
            TextPrefix = textPrefix,
            TextHighlight = textHighlight,
            GradientFrom = gradientFrom,
            GradientTo = gradientTo
        };

    /// <summary>
    /// Creates a logo using inline SVG markup next to styled text.
    /// </summary>
    public static AuthifySvgTextLogo FromSvg(
        string svgContent,
        string textPrefix,
        string textHighlight)
        => new()
        {
            SvgContent = svgContent,
            TextPrefix = textPrefix,
            TextHighlight = textHighlight
        };

    /// <summary>
    /// Creates a logo using an image (PNG, JPG or SVG file).
    /// </summary>
    public static AuthifyImageLogo FromImage(
        string imageUrl,
        string? altText = null,
        string? cssClass = null)
        => new()
        {
            ImageUrl = imageUrl,
            AltText = altText,
            CssClass = cssClass
        };
}

/// <summary>
/// Logo consisting of a FontAwesome icon in a gradient box and two-part text.
/// This is the default Authify logo style.
/// </summary>
public sealed class AuthifyIconTextLogo : AuthifyLogoOptions
{
    /// <summary>FontAwesome icon class, e.g. "fa-solid fa-shield-halved".</summary>
    public string IconClass { get; set; } = "fa-solid fa-shield-halved";

    /// <summary>Tailwind gradient-from class for the icon box, e.g. "auth-from-primary-500".</summary>
    public string GradientFrom { get; set; } = "auth-from-primary-500";

    /// <summary>Tailwind gradient-to class for the icon box, e.g. "auth-to-indigo-700".</summary>
    public string GradientTo { get; set; } = "auth-to-indigo-700";

    /// <summary>Plain text part of the brand name, e.g. "Auth".</summary>
    public string TextPrefix { get; set; } = "Auth";

    /// <summary>Highlighted (primary-colored) text part, e.g. "ify".</summary>
    public string TextHighlight { get; set; } = "ify";
}

/// <summary>
/// Logo consisting of an inline SVG graphic and two-part text.
/// </summary>
public sealed class AuthifySvgTextLogo : AuthifyLogoOptions
{
    /// <summary>Raw SVG markup rendered inside the icon container.</summary>
    public string SvgContent { get; set; } = string.Empty;

    /// <summary>Plain text part of the brand name.</summary>
    public string TextPrefix { get; set; } = string.Empty;

    /// <summary>Highlighted (primary-colored) text part.</summary>
    public string TextHighlight { get; set; } = string.Empty;
}

/// <summary>
/// Logo using a single image file (PNG, JPG or SVG). The image is displayed at a fixed height.
/// </summary>
public sealed class AuthifyImageLogo : AuthifyLogoOptions
{
    /// <summary>URL or relative path to the logo image.</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Alt text for accessibility. Defaults to the AppName.</summary>
    public string? AltText { get; set; }

    /// <summary>Additional CSS classes applied to the &lt;img&gt; element.</summary>
    public string? CssClass { get; set; }
}
