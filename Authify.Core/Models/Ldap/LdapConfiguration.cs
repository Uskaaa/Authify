namespace Authify.Core.Models.Ldap;

public class LdapConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Das Team, dem diese LDAP-Konfiguration gehört.
    /// Beim JIT-Provisioning wird der neue User diesem Team zugeordnet.
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// E-Mail-Domain der Unternehmensbenutzer (z.B. "company.com" oder "corp.internal").
    /// Wird beim Login verwendet um die passende LDAP-Konfiguration zu finden.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Hostname oder IP des LDAP-/Active Directory-Servers.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>LDAP-Port. Standard: 389 (unverschlüsselt) oder 636 (LDAPS).</summary>
    public int Port { get; set; } = 389;

    /// <summary>Ob LDAPS (SSL/TLS) verwendet werden soll.</summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Distinguished Name des Service-Accounts für die initiale Suche.
    /// Beispiel: "CN=ldap-service,OU=ServiceAccounts,DC=company,DC=com"
    /// </summary>
    public string BindDn { get; set; } = string.Empty;

    /// <summary>Passwort des Service-Accounts — verschlüsselt gespeichert (DataProtection API).</summary>
    public string BindPasswordEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// LDAP-Suchbasis. Beispiel: "DC=company,DC=com"
    /// </summary>
    public string BaseDn { get; set; } = string.Empty;

    /// <summary>
    /// LDAP-Suchfilter. {0} wird durch die E-Mail ersetzt.
    /// Standard für Active Directory: "(&(objectClass=user)(mail={0}))"
    /// Alternativ mit sAMAccountName: "(&(objectClass=user)(sAMAccountName={0}))"
    /// </summary>
    public string SearchFilter { get; set; } = "(&(objectClass=user)(mail={0}))";

    /// <summary>LDAP-Attribut für die E-Mail-Adresse. Standard: "mail"</summary>
    public string EmailAttribute { get; set; } = "mail";

    /// <summary>LDAP-Attribut für den Anzeigenamen. Standard: "displayName"</summary>
    public string DisplayNameAttribute { get; set; } = "displayName";

    /// <summary>Ob diese LDAP-Konfiguration aktiv ist.</summary>
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
