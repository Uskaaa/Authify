using Authify.Core.Models.Enums;

namespace Authify.Core.Models;

public class TwoFactorRequest
{
    public string UserId { get; set; }

    // Die 2FA-Methode, z.B. Email oder SMS
    public TwoFactorMethod TwoFactorMethod { get; set; }

    // Ob die Methode aktiviert oder deaktiviert wird
    public bool IsEnabled { get; set; } = true;

    // Priorität: niedriger Wert = höhere Priorität
    public int Priority { get; set; } = 0;
}