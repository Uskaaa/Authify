using Authify.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Authify.Application.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IEmailSender _emailSender;
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);

    public OtpService(IMemoryCache memoryCache, IEmailSender emailSender)
    {
        _memoryCache = memoryCache;
        _emailSender = emailSender;
    }

    public async Task GenerateAndSendOtpAsync(string email)
    {
        var otp = new Random().Next(100000, 999999).ToString();

        _memoryCache.Set($"otp_{email}", otp, _otpExpiration);

        await _emailSender.SendEmailAsync(email, "Your OTP Code", $"Your OTP code is: {otp}");
    }

    public async Task<bool> ValidateOtpAsync(string email, string otpCode)
    {
        if (_memoryCache.TryGetValue($"otp_{email}", out string? cachedOtp))
        {
            if (cachedOtp == otpCode)
            {
                _memoryCache.Remove($"otp_{email}");
                return true;
            }
        }

        return false;
    }
}