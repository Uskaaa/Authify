using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    public string NewPassword { get; set; } = string.Empty;
    
}