namespace Authify.Core.Models;

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public RefreshTokenRequest RefreshToken { get; set; }
}