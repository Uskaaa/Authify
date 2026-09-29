namespace Authify.UI.Models;

/// <summary>Well-known group labels of the profile navigation. Items of other groups get their own section.</summary>
public static class ProfileMenuGroups
{
    public const string AccountSettings = "Account Settings";
    public const string Subscription = "Subscription";
    public const string Admin = "Admin-Einstellungen";
}

/// <summary>
/// A navigation entry that another library contributes to the profile area (ProfileNavMenu /
/// ProfileLayout) through <see cref="IProfileMenuContributor"/>. Authify does not need to know the
/// contributing library or its pages.
/// </summary>
public class ProfileMenuItem
{
    /// <summary>Base-relative route without leading slash, e.g. <c>billing-overview</c>.</summary>
    public string Href { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Title shown in the top bar of ProfileLayout. Falls back to <see cref="Title"/>.</summary>
    public string? PageTitle { get; set; }

    /// <summary>Font Awesome classes, e.g. <c>fa-solid fa-receipt</c>.</summary>
    public string IconClass { get; set; } = string.Empty;

    public int Order { get; set; } = 0;

    /// <summary>Section label. Use <see cref="ProfileMenuGroups"/> to join an existing section.</summary>
    public string Group { get; set; } = ProfileMenuGroups.AccountSettings;

    /// <summary>Visibility predicate. Null means always visible.</summary>
    public Func<ProfileMenuContext, bool>? IsVisible { get; set; }
}
