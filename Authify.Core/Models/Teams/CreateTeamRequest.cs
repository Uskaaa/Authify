using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models.Teams;

public class CreateTeamRequest
{
    [Required(ErrorMessage = "Bitte gib einen Teamnamen ein")]
    [MinLength(2, ErrorMessage = "Der Teamname muss mindestens 2 Zeichen lang sein")]
    [MaxLength(100, ErrorMessage = "Der Teamname darf maximal 100 Zeichen lang sein")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Die Beschreibung darf maximal 500 Zeichen lang sein")]
    public string? Description { get; set; }

    [MaxLength(300, ErrorMessage = "Die Adresse darf maximal 300 Zeichen lang sein")]
    public string? CompanyAddress { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }
}
