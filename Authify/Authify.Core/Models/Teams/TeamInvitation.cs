namespace Authify.Core.Models.Teams;

public class TeamInvitation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// 32-Byte kryptographisch zufälliges Token (Base64Url-codiert, ~43 Zeichen).
    /// Nicht erratbar – 2^256 Möglichkeiten.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Wenn gesetzt: Einladung gilt nur für diese E-Mail-Adresse.
    /// Wenn null: Offene Einladung für beliebige Personen.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Maximale Anzahl Verwendungen. Null = unbegrenzt.
    /// </summary>
    public int? MaxUses { get; set; }

    public int UsedCount { get; set; } = 0;
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public Team? Team { get; set; }
}
