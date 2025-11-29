namespace Authify.UI.Models;

public class UserDeletionRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}