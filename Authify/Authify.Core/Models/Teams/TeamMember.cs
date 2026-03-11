namespace Authify.Core.Models.Teams;

public class TeamMember
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TeamId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public Team? Team { get; set; }
}

public enum TeamMemberRole
{
    Member = 0,
    Admin = 1
}
