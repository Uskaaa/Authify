namespace Authify.UI.Models;

public class AuthifyNavigationOptions
{
    /// <summary>
    /// URL to navigate to after successful login/register/OTP when no returnUrl is specified.
    /// Defaults to "/" (root). Host applications should override this (e.g. "/dashboard", "/admin").
    /// </summary>
    public string PostLoginUrl { get; set; } = "/";
}
