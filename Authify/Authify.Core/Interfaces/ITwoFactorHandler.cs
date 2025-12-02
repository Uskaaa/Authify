using Microsoft.AspNetCore.Identity;

namespace Authify.Core.Interfaces;

public interface ITwoFactorHandler<TUser>
{
    Task SendOtpAsync(TUser user, string otp);
}
