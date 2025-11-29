using System.ComponentModel.DataAnnotations;

namespace Authify.UI.Models;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = string.Empty;
}