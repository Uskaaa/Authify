using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Models.Enums;

namespace Authify.Core.Interfaces;

public interface ITwoFactorClaimService
{
    Task<OperationResult> AddClaimAsync(TwoFactorRequest request);
    Task<OperationResult> CheckClaimsAsync(string userId);
    Task<OperationResult> RemoveClaimAsync(TwoFactorRequest request);
}