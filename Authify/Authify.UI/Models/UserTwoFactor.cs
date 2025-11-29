using System.ComponentModel.DataAnnotations;
using Authify.UI.Models.Enums;

namespace Authify.UI.Models;

public class UserTwoFactor
{
    [Key]
    public string UserId { get; set; }
    public TwoFactorMethod Method { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 0;
}