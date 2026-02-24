using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IExternalLoginManagementService
{
    Task<OperationResult<IList<ExternalLoginDto>>> GetConnectedProvidersAsync(string userId);
    Task<OperationResult> DisconnectProviderAsync(string userId, string provider);
    Task<OperationResult> ConnectProviderAsync(string userId, ConnectExternalLoginRequest loginRequest);
}