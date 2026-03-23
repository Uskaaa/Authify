namespace Authify.Core.Models;

public class ResolvePersonalAccessTokenResponse
{
    public string TenantId { get; set; } = string.Empty;
    public string EndUserId { get; set; } = string.Empty;
}
