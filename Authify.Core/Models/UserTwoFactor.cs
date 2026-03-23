using System.ComponentModel.DataAnnotations;
using Authify.Core.Models.Enums;
namespace Authify.Core.Models;

public class UserTwoFactor
{
    [Key]
    public int Id { get; set; }
    public string UserId { get; set; }
    public TwoFactorMethod Method { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 0;
}