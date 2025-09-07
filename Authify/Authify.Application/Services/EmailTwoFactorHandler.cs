using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class EmailTwoFactorHandler<TUser> : ITwoFactorHandler<TUser>
    where TUser : IdentityUser
{
    private readonly IEmailSender _emailSender;

    public EmailTwoFactorHandler(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task SendOtpAsync(TUser user, string otp)
    {
        if (user.Email != null)
            await _emailSender.SendEmailAsync(user.Email, "Your OTP Code", $"Your OTP code is: {otp}");
    }
}