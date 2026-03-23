namespace Authify.Core.Models.Ldap;

public class LdapConfigurationDto
{
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    public string BindDn { get; set; } = string.Empty;

    /// <summary>Gibt nur an ob ein Passwort konfiguriert ist — niemals das Passwort selbst.</summary>
    public bool HasBindPassword { get; set; }

    public string BaseDn { get; set; } = string.Empty;
    public string SearchFilter { get; set; } = string.Empty;
    public string EmailAttribute { get; set; } = string.Empty;
    public string DisplayNameAttribute { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
