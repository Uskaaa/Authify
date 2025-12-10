using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class ExternalLoginManagementService<TUser> : IExternalLoginManagementService
    where TUser : ApplicationUser
{
    private readonly UserManager<TUser> _userManager;

    public ExternalLoginManagementService(UserManager<TUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<OperationResult<IList<ExternalLoginDto>>> GetConnectedProvidersAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult<IList<ExternalLoginDto>>.Fail("User not found");

        var logins = await _userManager.GetLoginsAsync(user);
        
        List<ExternalLoginDto> externalLogins = logins
            .Select(x => new ExternalLoginDto
            {
                LoginProvider = x.LoginProvider,
                ProviderKey = x.ProviderKey,
                ProviderDisplayName = x.ProviderDisplayName
            })
            .ToList();
        
        return OperationResult<IList<ExternalLoginDto>>.Ok(externalLogins);
    }

    private async Task<bool> CanDisconnectProviderAsync(TUser user, string provider)
    {
        var logins = await _userManager.GetLoginsAsync(user);
        bool hasOtherLogin = logins.Count > 1;
        bool hasPassword = await _userManager.HasPasswordAsync(user);

        return hasOtherLogin || hasPassword;
    }

    public async Task<OperationResult> DisconnectProviderAsync(string userId, string provider)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found");

        if (!await CanDisconnectProviderAsync(user, provider))
            return OperationResult.Fail("Cannot remove the last login provider without setting a password first.");

        var userLoginInfos = await _userManager.GetLoginsAsync(user);
        var loginInfo = userLoginInfos.FirstOrDefault(p => 
            string.Equals(p.LoginProvider, provider, StringComparison.OrdinalIgnoreCase));

        if (loginInfo == null)
            return OperationResult.Fail("Provider not found");
    
        // Remove via UserManager
        var removeResult = await _userManager.RemoveLoginAsync(user, provider, loginInfo.ProviderKey);
        if (!removeResult.Succeeded)
            return OperationResult.Fail(removeResult.Errors.FirstOrDefault()?.Description ?? "Unknown error");
        
        await _userManager.UpdateSecurityStampAsync(user);
        
        return OperationResult.Ok();
    }

    public async Task<OperationResult> ConnectProviderAsync(string userId, ConnectExternalLoginRequest loginRequest)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found");

        var logins = await _userManager.GetLoginsAsync(user);
        if (logins.Any(p => p.LoginProvider == loginRequest.LoginProvider))
            return OperationResult.Fail("Provider already connected");

        var userLoginInfo = new UserLoginInfo(
            loginRequest.LoginProvider,
            loginRequest.ProviderKey,
            loginRequest.ProviderDisplayName
        );

        var result = await _userManager.AddLoginAsync(user, userLoginInfo);
        if (!result.Succeeded)
            return OperationResult.Fail(result.Errors.FirstOrDefault()?.Description ?? "Unknown error");

        return OperationResult.Ok();
    }
}