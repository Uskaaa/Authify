namespace Authify.Core.Models.Teams;

public class TeamDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CompanyAddress { get; set; }
    public string? Website { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
}
