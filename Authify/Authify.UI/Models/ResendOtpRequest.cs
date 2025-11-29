using Authify.UI.Models.Enums;

namespace Authify.UI.Models;

public class ResendOtpRequest
{
    // Token aus LoginAsync
    public string Token { get; set; }

    // Username oder Email des Users
    public string UsernameOrEmail { get; set; }

    // Gewünschte 2FA Methode (falls alternative gewählt wird)
    public TwoFactorMethod TwoFactorMethod { get; set; }
}