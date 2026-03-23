using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models.Ldap;

public class LdapTestConnectionRequest
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 389;

    public bool UseSsl { get; set; } = false;

    [Required]
    public string BindDn { get; set; } = string.Empty;

    /// <summary>
    /// Klartext-Passwort für den Test. Wird nicht gespeichert.
    /// Bei einem bestehenden Config-Test kann leer gelassen werden —
    /// dann wird das gespeicherte (verschlüsselte) Passwort verwendet.
    /// </summary>
    public string? BindPassword { get; set; }

    /// <summary>
    /// Falls gesetzt, wird das gespeicherte Passwort der angegebenen Konfiguration verwendet.
    /// </summary>
    public string? ExistingConfigId { get; set; }

    [Required]
    public string BaseDn { get; set; } = string.Empty;
}
