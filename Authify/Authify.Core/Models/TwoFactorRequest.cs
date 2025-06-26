using Authify.Core.Models.Enums;

namespace Authify.Core.Models;

public class TwoFactorRequest
{
    public string UserId { get; set; }
    public TwoFactorMethod TwoFactorMethod { get; set; }
}