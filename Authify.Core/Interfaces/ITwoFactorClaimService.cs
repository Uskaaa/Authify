using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Models.Enums;

namespace Authify.Core.Interfaces;

public interface ITwoFactorClaimService
{
    Task<OperationResult> AddOrUpdateAsync(string userId, TwoFactorRequest request);
    Task<OperationResult> RemoveAsync(string userId, TwoFactorRequest request);
    Task<OperationResult<List<UserTwoFactor>>> GetAllAsync(string userId);
    Task<OperationResult<UserTwoFactor>> GetPreferredAsync(string userId);

    /// <summary>Generates (or re-fetches) the user's authenticator-app secret and returns setup data (QR + manual key).</summary>
    Task<OperationResult<TotpSetupInfo>> GetTotpSetupInfoAsync(string userId);

    /// <summary>Verifies a code from the user's authenticator app against the pending secret and, if valid, enables TOTP as a 2FA method.</summary>
    Task<OperationResult> ConfirmTotpAsync(string userId, string code);
}