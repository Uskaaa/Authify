using Authify.Core.Models.Enums;

namespace Authify.Core.Models;


public class UserTwoFactor
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; }
    public IdentityUser User { get; set; }

    public TwoFactorMethod Method { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int Priority { get; set; } = 0;
}