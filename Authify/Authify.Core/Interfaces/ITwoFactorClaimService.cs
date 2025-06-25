using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface ITwoFactorClaimService
{
    Task<OperationResult> AddClaimAsync(string userId, string claimType, string claimValue);
    Task<OperationResult> CheckClaimsAsync(string userId);
    Task<OperationResult> RemoveClaimAsync(string userId, string claimType, string claimValue);
}