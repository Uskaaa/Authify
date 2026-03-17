namespace Authify.Core.Models;

public class CreatePersonalAccessTokenRequest
{
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string EndUserId { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = [];
    public DateTimeOffset? ExpiresAt { get; set; }
}
