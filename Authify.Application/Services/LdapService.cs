using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models.Ldap;
using Authify.Core.Models.Teams;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Novell.Directory.Ldap;

namespace Authify.Application.Services;

public class LdapService<TUser> : ILdapService
    where TUser : ApplicationUser, new()
{
    private readonly ILdapDbContext _db;
    private readonly ITeamDbContext _teamDb;
    private readonly UserManager<TUser> _userManager;
    private readonly IDataProtector _protector;

    private const string ProtectorPurpose = "Authify.Ldap.BindPassword";

    public LdapService(ILdapDbContext db, ITeamDbContext teamDb,
        UserManager<TUser> userManager, IDataProtectionProvider dataProtectionProvider)
    {
        _db = db;
        _teamDb = teamDb;
        _userManager = userManager;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    }

    // ── Konfiguration abrufen ────────────────────────────────────────────────

    public async Task<LdapConfiguration?> GetConfigurationForDomainAsync(string domain)
    {
        return await _db.LdapConfigurations
            .FirstOrDefaultAsync(c => c.Domain == domain && c.IsEnabled);
    }

    public async Task<OperationResult<List<LdapConfigurationDto>>> GetConfigurationsForTeamAsync(string teamId)
    {
        var configs = await _db.LdapConfigurations
            .Where(c => c.TeamId == teamId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return OperationResult<List<LdapConfigurationDto>>.Ok(configs.Select(MapToDto).ToList());
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    public async Task<OperationResult<LdapConfigurationDto>> CreateConfigurationAsync(
        string teamId, CreateLdapConfigurationRequest request)
    {
        var domainExists = await _db.LdapConfigurations
            .AnyAsync(c => c.TeamId == teamId && c.Domain == request.Domain);
        if (domainExists)
            return OperationResult<LdapConfigurationDto>.Fail(
                $"Für die Domain '{request.Domain}' existiert bereits eine LDAP-Konfiguration in diesem Team.");

        var config = new LdapConfiguration
        {
            TeamId = teamId,
            Domain = request.Domain.ToLowerInvariant().Trim(),
            Host = request.Host,
            Port = request.Port,
            UseSsl = request.UseSsl,
            BindDn = request.BindDn,
            BindPasswordEncrypted = _protector.Protect(request.BindPassword),
            BaseDn = request.BaseDn,
            SearchFilter = string.IsNullOrWhiteSpace(request.SearchFilter)
                ? "(&(objectClass=user)(mail={0}))"
                : request.SearchFilter,
            EmailAttribute = string.IsNullOrWhiteSpace(request.EmailAttribute)
                ? "mail"
                : request.EmailAttribute,
            DisplayNameAttribute = string.IsNullOrWhiteSpace(request.DisplayNameAttribute)
                ? "displayName"
                : request.DisplayNameAttribute,
            IsEnabled = request.IsEnabled
        };

        _db.LdapConfigurations.Add(config);
        await _db.SaveChangesAsync();

        return OperationResult<LdapConfigurationDto>.Ok(MapToDto(config));
    }

    public async Task<OperationResult<LdapConfigurationDto>> UpdateConfigurationAsync(
        string configId, string teamId, UpdateLdapConfigurationRequest request)
    {
        var config = await _db.LdapConfigurations
            .FirstOrDefaultAsync(c => c.Id == configId && c.TeamId == teamId);
        if (config == null)
            return OperationResult<LdapConfigurationDto>.Fail("LDAP-Konfiguration nicht gefunden.");

        // Domain-Duplikat prüfen (nur wenn Domain geändert)
        if (!string.Equals(config.Domain, request.Domain, StringComparison.OrdinalIgnoreCase))
        {
            var domainExists = await _db.LdapConfigurations
                .AnyAsync(c => c.TeamId == teamId && c.Domain == request.Domain && c.Id != configId);
            if (domainExists)
                return OperationResult<LdapConfigurationDto>.Fail(
                    $"Für die Domain '{request.Domain}' existiert bereits eine LDAP-Konfiguration in diesem Team.");
        }

        config.Domain = request.Domain.ToLowerInvariant().Trim();
        config.Host = request.Host;
        config.Port = request.Port;
        config.UseSsl = request.UseSsl;
        config.BindDn = request.BindDn;
        config.BaseDn = request.BaseDn;
        config.SearchFilter = string.IsNullOrWhiteSpace(request.SearchFilter)
            ? "(&(objectClass=user)(mail={0}))"
            : request.SearchFilter;
        config.EmailAttribute = string.IsNullOrWhiteSpace(request.EmailAttribute)
            ? "mail"
            : request.EmailAttribute;
        config.DisplayNameAttribute = string.IsNullOrWhiteSpace(request.DisplayNameAttribute)
            ? "displayName"
            : request.DisplayNameAttribute;
        config.IsEnabled = request.IsEnabled;
        config.UpdatedAt = DateTime.UtcNow;

        // Passwort nur aktualisieren wenn befüllt
        if (!string.IsNullOrWhiteSpace(request.BindPassword))
            config.BindPasswordEncrypted = _protector.Protect(request.BindPassword);

        await _db.SaveChangesAsync();
        return OperationResult<LdapConfigurationDto>.Ok(MapToDto(config));
    }

    public async Task<OperationResult> DeleteConfigurationAsync(string configId, string teamId)
    {
        var config = await _db.LdapConfigurations
            .FirstOrDefaultAsync(c => c.Id == configId && c.TeamId == teamId);
        if (config == null)
            return OperationResult.Fail("LDAP-Konfiguration nicht gefunden.");

        _db.LdapConfigurations.Remove(config);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    // ── LDAP Test-Verbindung ─────────────────────────────────────────────────

    public async Task<LdapTestConnectionResult> TestConnectionAsync(LdapTestConnectionRequest request)
    {
        string bindPassword;

        if (!string.IsNullOrWhiteSpace(request.BindPassword))
        {
            bindPassword = request.BindPassword;
        }
        else if (!string.IsNullOrEmpty(request.ExistingConfigId))
        {
            var existing = await _db.LdapConfigurations
                .FirstOrDefaultAsync(c => c.Id == request.ExistingConfigId);
            if (existing == null)
                return LdapTestConnectionResult.Fail("Konfiguration nicht gefunden.");
            try { bindPassword = _protector.Unprotect(existing.BindPasswordEncrypted); }
            catch { return LdapTestConnectionResult.Fail("Gespeichertes Passwort konnte nicht entschlüsselt werden."); }
        }
        else
        {
            return LdapTestConnectionResult.Fail("Kein BindPassword angegeben.");
        }

        try
        {
            using var conn = new LdapConnection { SecureSocketLayer = request.UseSsl };
            conn.Connect(request.Host, request.Port);
            conn.Bind(request.BindDn, bindPassword);

            // Einfache Suche um zu prüfen ob BaseDn erreichbar
            var results = conn.Search(
                request.BaseDn,
                LdapConnection.ScopeSub,
                "(objectClass=*)",
                new[] { "dn" },
                typesOnly: false);

            int count = 0;
            while (results.HasMore() && count < 5)
            {
                try { results.Next(); count++; }
                catch (LdapReferralException) { break; }
            }

            return LdapTestConnectionResult.Ok(count);
        }
        catch (LdapException ex)
        {
            return LdapTestConnectionResult.Fail($"LDAP-Fehler ({ex.ResultCode}): {ex.Message}");
        }
        catch (Exception ex)
        {
            return LdapTestConnectionResult.Fail($"Verbindungsfehler: {ex.Message}");
        }
    }

    // ── LDAP Authentifizierung (Search-Bind-Pattern) ─────────────────────────

    public async Task<(bool Success, string? DisplayName, string? ErrorMessage)> AuthenticateAsync(
        string email, string password, LdapConfiguration config)
    {
        string bindPassword;
        try { bindPassword = _protector.Unprotect(config.BindPasswordEncrypted); }
        catch { return (false, null, "LDAP-Konfiguration konnte nicht geladen werden."); }

        return await Task.Run<(bool Success, string? DisplayName, string? ErrorMessage)>(() =>
        {
            try
            {
                using var conn = new LdapConnection { SecureSocketLayer = config.UseSsl };
                conn.Connect(config.Host, config.Port);

                // Schritt 1: Service-Account bind um User-DN zu suchen
                conn.Bind(config.BindDn, bindPassword);

                var searchFilter = string.Format(config.SearchFilter, LdapEscape(email));
                var results = conn.Search(
                    config.BaseDn,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    new[] { "dn", config.DisplayNameAttribute },
                    typesOnly: false);

                if (!results.HasMore())
                    return (false, null, "Benutzer im LDAP-Verzeichnis nicht gefunden.");

                LdapEntry userEntry;
                try { userEntry = results.Next(); }
                catch (LdapReferralException) { return (false, null, "LDAP-Referral konnte nicht aufgelöst werden."); }

                var userDn = userEntry.Dn;
                string? displayName = null;
                try { displayName = userEntry.GetAttribute(config.DisplayNameAttribute)?.StringValue; }
                catch { /* Attribut optional */ }

                // Schritt 2: Re-Bind mit den echten Benutzer-Credentials
                conn.Bind(userDn, password);

                return (true, displayName, null);
            }
            catch (LdapException ex) when (ex.ResultCode == LdapException.InvalidCredentials)
            {
                return (false, null, "Ungültige Anmeldedaten.");
            }
            catch (LdapException ex)
            {
                return (false, null, $"LDAP-Fehler ({ex.ResultCode}): {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, null, $"Verbindungsfehler: {ex.Message}");
            }
        });
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private static LdapConfigurationDto MapToDto(LdapConfiguration config) => new()
    {
        Id = config.Id,
        TeamId = config.TeamId,
        Domain = config.Domain,
        Host = config.Host,
        Port = config.Port,
        UseSsl = config.UseSsl,
        BindDn = config.BindDn,
        HasBindPassword = !string.IsNullOrEmpty(config.BindPasswordEncrypted),
        BaseDn = config.BaseDn,
        SearchFilter = config.SearchFilter,
        EmailAttribute = config.EmailAttribute,
        DisplayNameAttribute = config.DisplayNameAttribute,
        IsEnabled = config.IsEnabled,
        CreatedAt = config.CreatedAt,
        UpdatedAt = config.UpdatedAt
    };

    /// <summary>
    /// Escaped Sonderzeichen im LDAP-Suchfilter gemäß RFC 4515.
    /// </summary>
    private static string LdapEscape(string input) =>
        input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");

    // ── JIT Provisioning ─────────────────────────────────────────────────────

    public async Task<OperationResult<string>> EnsureUserProvisionedAsync(
        string email, string? displayName, LdapConfiguration config)
    {
        // User bereits vorhanden?
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            // Sicherstellen dass das LDAP-Flag gesetzt ist
            if (!existing.IsLdapUser)
            {
                existing.IsLdapUser = true;
                existing.LdapDomain = config.Domain;
                await _userManager.UpdateAsync(existing);
            }

            // Team-Mitgliedschaft sicherstellen
            await EnsureTeamMemberAsync(existing.Id, config.TeamId);
            return OperationResult<string>.Ok(existing.Id);
        }

        // Neuen User anlegen (JIT)
        var newUser = new TUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,  // LDAP-User brauchen keine E-Mail-Bestätigung
            FullName = displayName ?? email.Split('@')[0],
            IsLdapUser = true,
            LdapDomain = config.Domain
        };

        // Kein Passwort-Hash — LDAP-User authentifizieren sich immer per LDAP
        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return OperationResult<string>.Fail($"LDAP-User konnte nicht angelegt werden: {errors}");
        }

        await EnsureTeamMemberAsync(newUser.Id, config.TeamId);
        return OperationResult<string>.Ok(newUser.Id);
    }

    private async Task EnsureTeamMemberAsync(string userId, string teamId)
    {
        var alreadyMember = await _teamDb.TeamMembers
            .AnyAsync(m => m.UserId == userId && m.TeamId == teamId);
        if (alreadyMember) return;

        _teamDb.TeamMembers.Add(new TeamMember
        {
            TeamId = teamId,
            UserId = userId,
            Role = TeamMemberRole.Member
        });
        await _teamDb.SaveChangesAsync();
    }
}
