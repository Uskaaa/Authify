using Microsoft.AspNetCore.Identity;

namespace Authify.Core.Interfaces;

public interface IExternalLoginManagementService<TUser>
{
    Task<IList<UserLoginInfo>> GetConnectedProvidersAsync(TUser user);
    Task<bool> CanDisconnectProviderAsync(TUser user, string provider);
    Task<IdentityResult> DisconnectProviderAsync(TUser user, string provider);
    Task<IdentityResult> ConnectProviderAsync(TUser user, UserLoginInfo loginInfo);
}