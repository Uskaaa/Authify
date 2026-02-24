namespace Authify.Core.Models;

public class UserProfileDto
{
    public string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public string? FullName { get; set; }
    public string? JobTitle { get; set; }
    public string? Company { get; set; }
    public string? Bio { get; set; }
    public byte[]? ProfileImage { get; set; }
}
