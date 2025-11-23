namespace Authify.Core.Models;

/// <summary>
/// Request to connect an external login provider to the current user account.
/// </summary>
public class ConnectExternalLoginRequest
{
    /// <summary>
    /// The login provider (e.g., "Google", "GitHub")
    /// </summary>
    public string LoginProvider { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier for this login with the provider
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name for the provider
    /// </summary>
    public string? ProviderDisplayName { get; set; }
}

