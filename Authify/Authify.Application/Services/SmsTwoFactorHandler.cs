using Authify.Core.Interfaces;

namespace Authify.Application.Services;

public class SmsTwoFactorHandler : ITwoFactorHandler
{
    private readonly ISmsSender _smsSender;

    public SmsTwoFactorHandler(ISmsSender smsSender)
    {
        _smsSender = smsSender;
    }

    public async Task SendOtpAsync(string destination, string otp)
    {
        await _smsSender.SendSmsAsync(destination, $"Your OTP code is: {otp}");
    }
}