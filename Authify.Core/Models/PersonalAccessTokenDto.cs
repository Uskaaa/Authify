namespace Authify.Core.Models;

public class PersonalAccessTokenDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string EndUserId { get; set; } = string.Empty;
    public string TokenPrefix { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
