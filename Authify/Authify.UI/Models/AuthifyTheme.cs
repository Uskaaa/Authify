namespace Authify.UI.Models;

/// <summary>
/// Defines optional theme color overrides for Authify UI components.
/// Any property left null will fall back to the defaults defined in authify-theme.css.
/// </summary>
public class AuthifyTheme
{
    // ── Light mode ────────────────────────────────────────────────────────────

    /// <summary>Primary action color (light mode). Maps to --auth-primary.</summary>
    public string? Primary { get; set; }

    /// <summary>Primary hover color (light mode). Maps to --auth-primary-hover.</summary>
    public string? PrimaryHover { get; set; }

    /// <summary>Page background color (light mode). Maps to --auth-bg.</summary>
    public string? Background { get; set; }

    /// <summary>Card / surface background color (light mode). Maps to --auth-card-bg.</summary>
    public string? CardBackground { get; set; }

    /// <summary>Default text color (light mode). Maps to --auth-text.</summary>
    public string? Text { get; set; }

    /// <summary>Muted / secondary text color (light mode). Maps to --auth-text-muted.</summary>
    public string? TextMuted { get; set; }

    /// <summary>Input field background color (light mode). Maps to --auth-input-bg.</summary>
    public string? InputBackground { get; set; }

    /// <summary>Border color (light mode). Maps to --auth-border.</summary>
    public string? Border { get; set; }

    /// <summary>Error / danger color. Maps to --auth-error.</summary>
    public string? Error { get; set; }

    /// <summary>Success color. Maps to --auth-success.</summary>
    public string? Success { get; set; }

    /// <summary>Default box shadow. Maps to --auth-shadow.</summary>
    public string? Shadow { get; set; }

    // ── Dark mode ─────────────────────────────────────────────────────────────

    /// <summary>Primary action color (dark mode). Maps to --auth-primary inside :root.dark.</summary>
    public string? DarkPrimary { get; set; }

    /// <summary>Primary hover color (dark mode). Maps to --auth-primary-hover inside :root.dark.</summary>
    public string? DarkPrimaryHover { get; set; }

    /// <summary>Page background color (dark mode). Maps to --auth-bg inside :root.dark.</summary>
    public string? DarkBackground { get; set; }

    /// <summary>Card / surface background color (dark mode). Maps to --auth-card-bg inside :root.dark.</summary>
    public string? DarkCardBackground { get; set; }

    /// <summary>Default text color (dark mode). Maps to --auth-text inside :root.dark.</summary>
    public string? DarkText { get; set; }

    /// <summary>Muted / secondary text color (dark mode). Maps to --auth-text-muted inside :root.dark.</summary>
    public string? DarkTextMuted { get; set; }

    /// <summary>Input field background color (dark mode). Maps to --auth-input-bg inside :root.dark.</summary>
    public string? DarkInputBackground { get; set; }

    /// <summary>Border color (dark mode). Maps to --auth-border inside :root.dark.</summary>
    public string? DarkBorder { get; set; }

    /// <summary>Default box shadow (dark mode). Maps to --auth-shadow inside :root.dark.</summary>
    public string? DarkShadow { get; set; }

    /// <summary>Returns true when at least one light-mode property has been set.</summary>
    internal bool HasLightOverrides =>
        Primary is not null || PrimaryHover is not null ||
        Background is not null || CardBackground is not null ||
        Text is not null || TextMuted is not null ||
        InputBackground is not null || Border is not null ||
        Error is not null || Success is not null || Shadow is not null;

    /// <summary>Returns true when at least one dark-mode property has been set.</summary>
    internal bool HasDarkOverrides =>
        DarkPrimary is not null || DarkPrimaryHover is not null ||
        DarkBackground is not null || DarkCardBackground is not null ||
        DarkText is not null || DarkTextMuted is not null ||
        DarkInputBackground is not null || DarkBorder is not null || DarkShadow is not null;
}
