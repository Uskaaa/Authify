using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Server.Models;

public class RegisterRequest : IValidatableObject
{
    [Required(ErrorMessage = "Please enter your full name")]
    [MinLength(2, ErrorMessage = "Please enter your full name")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please enter your password")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please confirm your password")]
    public string ConfirmPassword { get; set; }
    
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions")]
    public bool TermsAccepted { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Password != ConfirmPassword)
        {
            yield return new ValidationResult("Passwords do not match", [nameof(ConfirmPassword)]);
        }
    }
}