namespace Authify.Core.Models;

/// <summary>
/// Intermediate result of credential/OTP validation (before SignInAsync is called).
/// Used by the HTTP redirect login flow so that the actual SignInAsync only happens
/// inside a real HTTP request context where Identity can write the HttpOnly cookie.
/// </summary>
public class PendingLoginResult
{
    public string? UserId { get; set; }
    public bool IsPersistent { get; set; }

    /// <summary>Set to true when a 2-FA OTP is required before sign-in can be completed.</summary>
    public bool RequiresOtp { get; set; }

    /// <summary>DataProtection-signed OTP token to pass to the OTP page.</summary>
    public string? OtpToken { get; set; }
}
