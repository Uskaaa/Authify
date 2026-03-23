namespace Authify.Core.Models.Teams;

public class Team
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CompanyAddress { get; set; }
    public string? Website { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<TeamInvitation> Invitations { get; set; } = new List<TeamInvitation>();
}
