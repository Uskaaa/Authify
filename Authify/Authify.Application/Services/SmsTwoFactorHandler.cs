using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class SmsTwoFactorHandler<TUser> : ITwoFactorHandler<TUser>
where TUser : IdentityUser
{
    private readonly ISmsSender _smsSender;

    public SmsTwoFactorHandler(ISmsSender smsSender)
    {
        _smsSender = smsSender;
    }

    public async Task SendOtpAsync(TUser user, string otp)
    {
        if (user.PhoneNumber != null) await _smsSender.SendSmsAsync(user.PhoneNumber, $"Your OTP code is: {otp}");
    }
}