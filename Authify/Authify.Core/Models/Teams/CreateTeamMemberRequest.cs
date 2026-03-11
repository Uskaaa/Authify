using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models.Teams;

public class CreateTeamMemberRequest
{
    [Required(ErrorMessage = "Bitte gib eine E-Mail-Adresse ein")]
    [EmailAddress(ErrorMessage = "Bitte gib eine gültige E-Mail-Adresse ein")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib den vollständigen Namen ein")]
    [MinLength(2, ErrorMessage = "Der Name muss mindestens 2 Zeichen lang sein")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Temporäres Passwort. Falls leer wird ein zufälliges generiert und
    /// der Nutzer muss es beim ersten Login ändern.
    /// </summary>
    public string? TemporaryPassword { get; set; }
}
