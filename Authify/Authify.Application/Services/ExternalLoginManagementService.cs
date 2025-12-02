using Authify.Application.Data;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class ExternalLoginManagementService<TUser> : IExternalLoginManagementService<TUser>
    where TUser : ApplicationUser
{
    private readonly UserManager<TUser> _userManager;

    public ExternalLoginManagementService(UserManager<TUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IList<UserLoginInfo>> GetConnectedProvidersAsync(TUser user)
    {
        return await _userManager.GetLoginsAsync(user);
    }

    public async Task<bool> CanDisconnectProviderAsync(TUser user, string provider)
    {
        var logins = await _userManager.GetLoginsAsync(user);
        bool hasOtherLogin = logins.Count > 1; // Mindestens ein anderer Provider
        bool hasPassword = await _userManager.HasPasswordAsync(user);

        // Nur erlauben, wenn danach noch Login-Möglichkeiten bestehen
        return hasOtherLogin || hasPassword;
    }

    public async Task<IdentityResult> DisconnectProviderAsync(TUser user, string provider)
    {
        if (!await CanDisconnectProviderAsync(user, provider))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "LastLoginRemoval",
                Description = "Cannot remove the last login provider without setting a password first."
            });
        }

        return await _userManager.RemoveLoginAsync(user, provider, user.Id);
    }

    public async Task<IdentityResult> ConnectProviderAsync(TUser user, UserLoginInfo loginInfo)
    {
        return await _userManager.AddLoginAsync(user, loginInfo);
    }
}