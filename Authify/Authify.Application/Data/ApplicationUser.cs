using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Data;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    /// <summary>
    /// Gibt an ob dieser User über LDAP authentifiziert wird.
    /// LDAP-User haben keinen lokalen PasswordHash — Login läuft immer über LDAP-Bind.
    /// </summary>
    public bool IsLdapUser { get; set; } = false;

    /// <summary>
    /// Die E-Mail-Domain des LDAP-Verzeichnisses (z.B. "company.com").
    /// Wird beim Login verwendet um die passende LdapConfiguration zu finden.
    /// </summary>
    public string? LdapDomain { get; set; }
}