using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Authify.Core.Models;

public class UserProfile
{
    [Key]
    public string UserId { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? JobTitle { get; set; }

    public string? Company { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    public byte[]? ProfileImage { get; set; } // Blob für Bild
}