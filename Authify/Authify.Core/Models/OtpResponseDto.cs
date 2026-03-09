namespace Authify.Core.Models;

public class OtpResponseDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    /// <summary>
    /// When set (server-side cookie mode), the client must navigate here (forceLoad)
    /// to complete sign-in and receive the HttpOnly auth cookie.
    /// </summary>
    public string? RedirectUrl { get; set; }
}