using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "Please enter your email address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
    public string? DeviceName { get; set; } = string.Empty;
    public string? IpAddress { get; set; } = string.Empty;
}