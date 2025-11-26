using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; }
    
    [Required(ErrorMessage = "Please enter your new password")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;
    
}