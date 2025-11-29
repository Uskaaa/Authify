using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Authify.Core.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace Authify.Core.Models;

public class UserTwoFactor
{
    [Key]
    public string UserId { get; set; }
    public TwoFactorMethod Method { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 0;
}