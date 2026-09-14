namespace Authify.UI.Services;

/// <summary>
/// Benachrichtigt die Sidebar-Navigation (ProfileNavMenu), wenn sich der Team-Status
/// eines Benutzers ändert (Team erstellt/gelöscht), damit die Admin-Navigationspunkte
/// live aktualisiert werden, ohne dass ein Seiten-Reload nötig ist.
/// </summary>
public class TeamNavStateService
{
    public event Action? OnChange;

    public void NotifyTeamChanged() => OnChange?.Invoke();
}
