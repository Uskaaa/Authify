using Authify.Core.Interfaces;

namespace Authify.Application.Services;

public class EmailTwoFactorHandler : ITwoFactorHandler
{
    private readonly IEmailSender _emailSender;

    public EmailTwoFactorHandler(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task SendOtpAsync(string destination, string otp)
    {
        await _emailSender.SendEmailAsync(destination, "Your OTP Code", $"Your OTP code is: {otp}");
    }
}