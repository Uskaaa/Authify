namespace Authify.Core.Models;

public class OtpPayload
{
    public string Email { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}