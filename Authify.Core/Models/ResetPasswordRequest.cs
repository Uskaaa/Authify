using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models;

public class ResetPasswordRequest : IValidatableObject
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please enter your new password")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please confirm your new password")]
    public string ConfirmNewPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NewPassword != ConfirmNewPassword)
        {
            yield return new ValidationResult("Passwords do not match", new[] { nameof(ConfirmNewPassword) });
        }
    }
}