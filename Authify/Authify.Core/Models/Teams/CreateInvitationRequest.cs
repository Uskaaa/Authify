namespace Authify.Core.Models.Teams;

public class CreateInvitationRequest
{
    /// <summary>
    /// Wenn gesetzt: Einladung gilt nur für diese eine E-Mail.
    /// Wenn null: Offene Einladung (für beliebige Personen).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Maximale Anzahl Einlösungen. Null = unbegrenzt.
    /// Bei einzelner E-Mail-Einladung wird automatisch 1 gesetzt.
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Gültigkeit in Tagen. Standard: 7 Tage.
    /// </summary>
    public int ExpirationDays { get; set; } = 7;
}
