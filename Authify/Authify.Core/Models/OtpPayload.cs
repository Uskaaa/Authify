using Authify.Core.Models.Enums;

namespace Authify.Core.Models;

public class OtpPayload
{
    public string Email { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public TwoFactorMethod Method { get; set; }
}