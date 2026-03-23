using Authify.Core.Common;
using Authify.Core.Models.Ldap;

namespace Authify.Core.Interfaces;

public interface ILdapService
{
    /// <summary>
    /// Sucht eine aktive LDAP-Konfiguration anhand der E-Mail-Domain.
    /// Wird beim Login aufgerufen um zu bestimmen ob LDAP-Auth verwendet werden soll.
    /// Gibt null zurück wenn LDAP nicht konfiguriert oder disabled.
    /// </summary>
    Task<LdapConfiguration?> GetConfigurationForDomainAsync(string domain);

    /// <summary>
    /// Gibt alle LDAP-Konfigurationen eines Teams zurück.
    /// </summary>
    Task<OperationResult<List<LdapConfigurationDto>>> GetConfigurationsForTeamAsync(string teamId);

    /// <summary>
    /// Erstellt eine neue LDAP-Konfiguration für ein Team.
    /// Das BindPassword wird vor dem Speichern verschlüsselt.
    /// </summary>
    Task<OperationResult<LdapConfigurationDto>> CreateConfigurationAsync(string teamId, CreateLdapConfigurationRequest request);

    /// <summary>
    /// Aktualisiert eine bestehende LDAP-Konfiguration.
    /// BindPassword nur aktualisiert wenn in request befüllt.
    /// </summary>
    Task<OperationResult<LdapConfigurationDto>> UpdateConfigurationAsync(string configId, string teamId, UpdateLdapConfigurationRequest request);

    /// <summary>
    /// Löscht eine LDAP-Konfiguration.
    /// </summary>
    Task<OperationResult> DeleteConfigurationAsync(string configId, string teamId);

    /// <summary>
    /// Testet ob eine Verbindung zum LDAP-Server mit den angegebenen Credentials hergestellt werden kann.
    /// Verwendet das gespeicherte Passwort wenn ExistingConfigId gesetzt und BindPassword leer.
    /// </summary>
    Task<LdapTestConnectionResult> TestConnectionAsync(LdapTestConnectionRequest request);

    /// <summary>
    /// Authentifiziert einen Benutzer gegen den LDAP-Server (Search-Bind-Pattern).
    /// Gibt bei Erfolg den Anzeigenamen zurück.
    /// </summary>
    Task<(bool Success, string? DisplayName, string? ErrorMessage)> AuthenticateAsync(
        string email, string password, LdapConfiguration config);

    /// <summary>
    /// JIT-Provisioning: Stellt sicher dass der LDAP-User in der lokalen Identity-DB existiert
    /// und dem richtigen Team zugeordnet ist.
    /// Gibt die UserId des (neu erstellten oder bestehenden) Users zurück.
    /// </summary>
    Task<OperationResult<string>> EnsureUserProvisionedAsync(
        string email, string? displayName, LdapConfiguration config);
}
