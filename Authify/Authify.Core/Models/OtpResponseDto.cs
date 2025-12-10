namespace Authify.Core.Models;

public class OtpResponseDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}