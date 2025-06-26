using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Models.Enums;

namespace Authify.Core.Interfaces;

public interface ITwoFactorClaimService
{
    Task<OperationResult> AddClaimAsync(string userId, TwoFactorMethod twoFactorMethod);
    Task<OperationResult> CheckClaimsAsync(string userId);
    Task<OperationResult> RemoveClaimAsync(string userId, TwoFactorMethod twoFactorMethod);
}