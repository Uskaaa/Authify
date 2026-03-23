using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models.Ldap;

namespace Authify.UI.Services;

/// <summary>
/// Fallback-Implementierung von ILdapDataService für Anwendungen ohne LDAP-Feature.
/// </summary>
public class NullLdapDataService : ILdapDataService
{
    public Task<OperationResult<List<LdapConfigurationDto>>> GetConfigurationsAsync() =>
        Task.FromResult(OperationResult<List<LdapConfigurationDto>>.Ok([]));

    public Task<OperationResult<LdapConfigurationDto>> CreateConfigurationAsync(CreateLdapConfigurationRequest request) =>
        Task.FromResult(OperationResult<LdapConfigurationDto>.Fail("LDAP-Feature ist nicht aktiviert."));

    public Task<OperationResult<LdapConfigurationDto>> UpdateConfigurationAsync(string configId, UpdateLdapConfigurationRequest request) =>
        Task.FromResult(OperationResult<LdapConfigurationDto>.Fail("LDAP-Feature ist nicht aktiviert."));

    public Task<OperationResult> DeleteConfigurationAsync(string configId) =>
        Task.FromResult(OperationResult.Fail("LDAP-Feature ist nicht aktiviert."));

    public Task<LdapTestConnectionResult> TestConnectionAsync(LdapTestConnectionRequest request) =>
        Task.FromResult(LdapTestConnectionResult.Fail("LDAP-Feature ist nicht aktiviert."));
}
