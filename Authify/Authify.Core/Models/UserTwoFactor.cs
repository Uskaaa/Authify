using Authify.Core.Models.Enums;
namespace Authify.Core.Models;

public class UserTwoFactor
{
    public string UserId { get; set; }
    public TwoFactorMethod Method { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 0;
}