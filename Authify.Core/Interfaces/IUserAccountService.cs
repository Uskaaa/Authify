using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IUserAccountService
{
    Task<OperationResult<byte[]>> RequestExportAsync(string userId);
    Task<OperationResult<UserExportRequest>> GetExportStatusAsync(string userId);

    Task<OperationResult> DeactivateAccountAsync(string userId);
    Task<OperationResult<UserDeactivationRequest>> GetDeactivationStatusAsync(string userId);

    Task<OperationResult> DeleteAccountAsync(string userId);
    Task<OperationResult<UserDeletionRequest>> GetDeletionStatusAsync(string userId);
}