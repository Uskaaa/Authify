using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models.Ldap;

namespace Authify.Application.Services;

/// <summary>
/// No-op Implementierung von ILdapService für Anwendungen ohne LDAP-Feature.
/// GetConfigurationForDomainAsync gibt immer null zurück → Login-Flow überspringt LDAP.
/// </summary>
public class NullLdapService : ILdapService
{
    public Task<LdapConfiguration?> GetConfigurationForDomainAsync(string domain)
        => Task.FromResult<LdapConfiguration?>(null);

    public Task<OperationResult<List<LdapConfigurationDto>>> GetConfigurationsForTeamAsync(string teamId)
        => Task.FromResult(OperationResult<List<LdapConfigurationDto>>.Ok([]));

    public Task<OperationResult<LdapConfigurationDto>> CreateConfigurationAsync(string teamId, CreateLdapConfigurationRequest request)
        => Task.FromResult(OperationResult<LdapConfigurationDto>.Fail("LDAP-Feature ist nicht aktiviert."));

    public Task<OperationResult<LdapConfigurationDto>> UpdateConfigurationAsync(string configId, string teamId, UpdateLdapConfigurationRequest request)
        => Task.FromResult(OperationResult<LdapConfigurationDto>.Fail("LDAP-Feature ist nicht aktiviert."));

    public Task<OperationResult> DeleteConfigurationAsync(string configId, string teamId)
        => Task.FromResult(OperationResult.Fail("LDAP-Feature ist nicht aktiviert."));

    public Task<LdapTestConnectionResult> TestConnectionAsync(LdapTestConnectionRequest request)
        => Task.FromResult(LdapTestConnectionResult.Fail("LDAP-Feature ist nicht aktiviert."));

    public Task<(bool Success, string? DisplayName, string? ErrorMessage)> AuthenticateAsync(
        string email, string password, LdapConfiguration config)
        => Task.FromResult((false, (string?)null, (string?)"LDAP-Feature ist nicht aktiviert."));

    public Task<OperationResult<string>> EnsureUserProvisionedAsync(
        string email, string? displayName, LdapConfiguration config)
        => Task.FromResult(OperationResult<string>.Fail("LDAP-Feature ist nicht aktiviert."));
}
