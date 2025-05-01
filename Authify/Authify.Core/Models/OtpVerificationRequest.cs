namespace Authify.Core.Models;

public class OtpVerificationRequest
{
    public string Token { get; set; }
    public string OtpCode { get; set; } = string.Empty;
}