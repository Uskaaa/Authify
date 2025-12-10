namespace Authify.Core.Models;

public class ConnectExternalLoginRequest
{
    public string LoginProvider { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string? ProviderDisplayName { get; set; }
}

