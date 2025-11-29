using Microsoft.AspNetCore.Identity;

namespace Authify.Core.Models;

public class UserDeactivationRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeactivated { get; set; } = false;
    public DateTime? DeactivatedAt { get; set; }
}