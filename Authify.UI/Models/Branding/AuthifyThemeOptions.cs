namespace Authify.UI.Models.Branding;

/// <summary>
/// Theme color configuration for Authify.UI.
/// Overrides CSS custom properties at runtime – both the semantic vars used by scoped CSS
/// and the per-shade RGB vars consumed by Tailwind utility classes (e.g. auth-bg-primary-600/20).
/// </summary>
public sealed class AuthifyThemeOptions
{
    /// <summary>
    /// Full primary color palette as hex values keyed by Tailwind shade (50 … 950).
    /// Override individual shades or replace the entire palette.
    /// Defaults to the Authify indigo palette.
    /// </summary>
    public Dictionary<int, string> PrimaryPalette { get; set; } = DefaultPrimaryPalette();

    // ── Light-mode semantic overrides ──────────────────────────────────────
    /// <summary>Page background in light mode. Defaults to primary-50.</summary>
    public string? LightBackground { get; set; }

    /// <summary>Card / surface background in light mode. Defaults to #ffffff.</summary>
    public string? LightCardBackground { get; set; }

    // ── Dark-mode semantic overrides ───────────────────────────────────────
    /// <summary>Page background in dark mode. Defaults to #020617 (slate-950).</summary>
    public string? DarkBackground { get; set; }

    /// <summary>Card / surface background in dark mode. Defaults to #0f172a (slate-900).</summary>
    public string? DarkCardBackground { get; set; }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Gets the hex value for a primary shade, falling back to the default palette.</summary>
    public string GetPrimaryHex(int shade)
        => PrimaryPalette.TryGetValue(shade, out var hex) ? hex : DefaultPrimaryPalette()[shade];

    /// <summary>
    /// Converts a hex color string (#rrggbb or #rgb) to space-separated RGB integers
    /// as required by the Tailwind CSS variable convention (e.g. "79 70 229").
    /// </summary>
    public static string HexToRgb(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);

        if (hex.Length != 6)
            throw new ArgumentException($"Invalid hex color: #{hex}");

        int r = Convert.ToInt32(hex[..2], 16);
        int g = Convert.ToInt32(hex[2..4], 16);
        int b = Convert.ToInt32(hex[4..6], 16);
        return $"{r} {g} {b}";
    }

    private static Dictionary<int, string> DefaultPrimaryPalette() => new()
    {
        [50]  = "#eef2ff",
        [100] = "#e0e7ff",
        [200] = "#c7d2fe",
        [300] = "#a5b4fc",
        [400] = "#818cf8",
        [500] = "#6366f1",
        [600] = "#4f46e5",
        [700] = "#4338ca",
        [800] = "#3730a3",
        [900] = "#312e81",
        [950] = "#1e1b4b"
    };
}
