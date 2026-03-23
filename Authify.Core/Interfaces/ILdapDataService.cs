using Authify.Core.Common;
using Authify.Core.Models.Ldap;

namespace Authify.Core.Interfaces;

/// <summary>
/// UI-seitiger Service für LDAP-Operationen. Wird von WasmLdapDataService und
/// ServerLdapDataService implementiert.
/// </summary>
public interface ILdapDataService
{
    Task<OperationResult<List<LdapConfigurationDto>>> GetConfigurationsAsync();
    Task<OperationResult<LdapConfigurationDto>> CreateConfigurationAsync(CreateLdapConfigurationRequest request);
    Task<OperationResult<LdapConfigurationDto>> UpdateConfigurationAsync(string configId, UpdateLdapConfigurationRequest request);
    Task<OperationResult> DeleteConfigurationAsync(string configId);
    Task<LdapTestConnectionResult> TestConnectionAsync(LdapTestConnectionRequest request);
}
