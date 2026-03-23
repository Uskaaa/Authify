namespace Authify.Core.Models.Teams;

public class TeamMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TeamMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    /// <summary>
    /// Nur bei neu erstellten Accounts befüllt (einmalig nach Erstellung).
    /// Null wenn der Nutzer bereits existierte oder eine E-Mail gesendet wurde.
    /// </summary>
    public string? TemporaryPassword { get; set; }
}
