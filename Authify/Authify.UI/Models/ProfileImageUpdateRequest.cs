namespace Authify.Core.Models;

public class ProfileImageUpdateRequest
{
    public byte[]? Image { get; set; } // null = löschen
}