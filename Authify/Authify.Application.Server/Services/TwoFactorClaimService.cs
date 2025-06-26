using System.Security.Claims;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class TwoFactorClaimService : ITwoFactorClaimService
{
    private readonly UserManager<IdentityUser> _userManager;
    
    public TwoFactorClaimService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<OperationResult> AddClaimAsync(string userId, TwoFactorMethod twoFactorMethod)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");
        
        var result = await _userManager.AddClaimAsync(user, new Claim("TwoFactor", twoFactorMethod.ToString()));
        
        return result.Succeeded
            ? OperationResult.Ok()
            : OperationResult.Fail("Failed to add claim.");
    }

    public async Task<OperationResult> CheckClaimsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");
        
        var claims = await _userManager.GetClaimsAsync(user);

        return claims.Count == 0 ? OperationResult.Fail("No claims found for the user.") : OperationResult<List<Claim>>.Ok(claims.ToList());
    }
    
    public async Task<OperationResult> RemoveClaimAsync(string userId, TwoFactorMethod twoFactorMethod)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");
        
        var claims = await _userManager.GetClaimsAsync(user);
        var claimToRemove = claims.FirstOrDefault(c => c.Type == "TwoFactor" && c.Value == twoFactorMethod.ToString());
        
        if (claimToRemove == null)
            return OperationResult.Fail("Claim not found.");
        

        var result = await _userManager.RemoveClaimAsync(user, claimToRemove);
        
        return result.Succeeded
            ? OperationResult.Ok()
            : OperationResult.Fail("Failed to remove claim.");
    }
}