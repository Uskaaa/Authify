using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models;

public class UserSession
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; }

    public string DeviceName { get; set; } // z.B. "MacBook Pro", "iPhone 15"

    public string IpAddress { get; set; }

    public string? Location { get; set; } // optional, z.B. "San Francisco, CA"

    public DateTime LastSeen { get; set; } // zuletzt aktiv

    public string AuthType { get; set; } // "Cookie", "JWT", "Google", "GitHub"...

    public bool IsActive { get; set; } = true; // false bei Logout oder Token-Expiry
}