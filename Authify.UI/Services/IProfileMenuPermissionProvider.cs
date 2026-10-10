namespace Authify.UI.Services;

/// <summary>
/// Optional source of the current user's team permissions for the profile navigation, so contributed
/// menu items can show up for team members who hold the matching permission (not only for the admin).
/// Authify itself defines no such permissions; a library that does (e.g. BillingEngine) registers one.
/// </summary>
public interface IProfileMenuPermissionProvider
{
    Task<IReadOnlyCollection<string>> GetPermissionsAsync();
}
