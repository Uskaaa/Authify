using Authify.Application.Data;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class SmsTwoFactorHandler<TUser> : ITwoFactorHandler<TUser>
where TUser : ApplicationUser
{
    private readonly ISmsSender _smsSender;

    public SmsTwoFactorHandler(ISmsSender smsSender)
    {
        _smsSender = smsSender;
    }

    public async Task SendOtpAsync(TUser user, string otp)
    {
        // Defense in depth: AddOrUpdateAsync already blocks enabling SMS 2FA on an unconfirmed
        // number, but re-check here too so a code never goes out to a number that was changed
        // (and thus un-confirmed) after SMS 2FA was already enabled.
        if (!string.IsNullOrEmpty(user.PhoneNumber) && user.PhoneNumberConfirmed)
            await _smsSender.SendSmsAsync(user.PhoneNumber, $"Your OTP code is: {otp}");
    }
}