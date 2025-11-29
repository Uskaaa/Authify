namespace Authify.UI.Models;

public class PersonalInformationUpdateRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? JobTitle { get; set; }
    public string? Company { get; set; }
    public string? Bio { get; set; }
}