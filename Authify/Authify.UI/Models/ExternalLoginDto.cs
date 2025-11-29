namespace Authify.UI.Models;

/// <summary>
/// Represents an external login provider connected to a user account.
/// Platform-independent alternative to UserLoginInfo.
/// </summary>
public class ExternalLoginDto
{
    /// <summary>
    /// The login provider (e.g., "Google", "GitHub", "Microsoft")
    /// </summary>
    public string LoginProvider { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier for this login with the provider
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// The display name for this login
    /// </summary>
    public string? ProviderDisplayName { get; set; }
}

