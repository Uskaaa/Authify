namespace Authify.UI.Models;

public class OtpVerificationRequest
{
    public string Token { get; set; }
    public string OtpCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "unknown";
    public string? IpAddress { get; set; }
}