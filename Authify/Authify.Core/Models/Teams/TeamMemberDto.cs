namespace Authify.Core.Models.Teams;

public class TeamMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TeamMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
