namespace Authify.Core.Models;

public class OtpVerificationRequest
{
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}