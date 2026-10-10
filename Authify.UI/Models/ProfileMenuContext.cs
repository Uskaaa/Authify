namespace Authify.UI.Models;

/// <summary>What ProfileNavMenu knows about the current user when it asks contributors what to show.</summary>
/// <param name="IsTeamAdmin">The user administers a team.</param>
/// <param name="IsTeamMember">The user is a non-admin member of a team (same meaning as in ProfileNavMenu).</param>
/// <param name="TeamFeatureEnabled">The team feature is switched on for this app.</param>
/// <param name="Permissions">Team permissions the user holds, as reported by the registered
/// <see cref="Authify.UI.Services.IProfileMenuPermissionProvider"/>s (empty if none is registered).</param>
public sealed record ProfileMenuContext(bool IsTeamAdmin, bool IsTeamMember, bool TeamFeatureEnabled,
    IReadOnlySet<string>? Permissions = null)
{
    public bool HasPermission(string permission) => Permissions?.Contains(permission) == true;
}
