namespace Authify.Core.Features;

/// <summary>
/// Steuert ob Team-Account-Features in der Anwendung aktiviert sind.
/// Wird als Singleton registriert. Wird von UI-Komponenten genutzt um
/// team-spezifische Navigation und Seiten ein/auszublenden.
/// </summary>
public class TeamFeatureOptions
{
    public bool IsEnabled { get; set; } = false;
}
