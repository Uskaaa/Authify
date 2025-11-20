// ...new file...
namespace Authify.Core.Models;

public enum LoginResultKind
{
    Unknown = 0,
    Jwt = 1,
    Cookie = 2
}

public class LoginResponseDto
{
    public LoginResultKind ResultKind { get; set; } = LoginResultKind.Unknown;

    // JWT
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? AccessTokenExpiresAt { get; set; }

    // Cookie
    public bool CookieSet { get; set; }
    public string? CookieName { get; set; }
    public bool IsPersistent { get; set; }

    // Generic
    public string? Message { get; set; }
}

