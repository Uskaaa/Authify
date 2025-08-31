using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace Authify.Core.Interfaces;

public interface ITwoFactorClaimService
{
    Task<OperationResult> AddOrUpdateAsync(string userId, TwoFactorRequest request);
    Task<OperationResult> RemoveAsync(string userId, TwoFactorRequest request);
    Task<OperationResult<List<UserTwoFactor>>> GetAllAsync(string userId);
    Task<OperationResult<UserTwoFactor>> GetPreferredAsync(string userId);
}