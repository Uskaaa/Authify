using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models.Teams;

public class UpdateTeamRequest
{
    [Required(ErrorMessage = "Bitte gib einen Teamnamen ein")]
    [MinLength(2)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(300)]
    public string? CompanyAddress { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }
}
