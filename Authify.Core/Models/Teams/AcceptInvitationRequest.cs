using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models.Teams;

public class AcceptInvitationRequest : IValidatableObject
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib deinen vollständigen Namen ein")]
    [MinLength(2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib eine E-Mail-Adresse ein")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte wähle ein Passwort")]
    [MinLength(6, ErrorMessage = "Das Passwort muss mindestens 6 Zeichen lang sein")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte bestätige dein Passwort")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Password != ConfirmPassword)
            yield return new ValidationResult("Die Passwörter stimmen nicht überein", [nameof(ConfirmPassword)]);
    }
}
