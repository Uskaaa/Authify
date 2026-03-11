namespace Authify.Core.Models.Teams;

public class TeamInvitationDto
{
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public bool IsValid => !IsRevoked && !IsExpired && (MaxUses == null || UsedCount < MaxUses);
}
