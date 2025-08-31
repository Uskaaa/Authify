namespace Authify.Core.Models;

public class RefreshToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional: IP/Device info für zusätzliche Sicherheit
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
}