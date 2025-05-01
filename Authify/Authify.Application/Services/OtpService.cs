using System.Text.Json;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

namespace Authify.Application.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IEmailSender _emailSender;
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public OtpService(IMemoryCache memoryCache, IEmailSender emailSender, IDataProtectionProvider dataProtectionProvider)
    {
        _memoryCache = memoryCache;
        _emailSender = emailSender;
        _dataProtectionProvider = dataProtectionProvider;
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
    
    // Methode zur Tokenverschlüsselung
    public string GenerateToken(string email, bool rememberMe)
    {
        OtpPayload otpPayload = new OtpPayload
        {
            Email = email,
            RememberMe = rememberMe
        };
        var payload = JsonSerializer.Serialize(otpPayload);
        var protector = _dataProtectionProvider.CreateProtector("otp-auth");
        return protector.Protect(payload);
    }

    // Methode zur Entschlüsselung eines Tokens
    public (string email, bool rememberMe) ValidateToken(string token)
    {
        var protector = _dataProtectionProvider.CreateProtector("otp-auth");
        var decryptedData = protector.Unprotect(token); // Entschlüsselt das Token
        var payload = JsonSerializer.Deserialize<OtpPayload>(decryptedData);

        return (payload.Email, payload.RememberMe);
    }
}