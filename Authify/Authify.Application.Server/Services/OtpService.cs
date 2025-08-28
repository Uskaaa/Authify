using System.Text.Json;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Models.Enums;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

namespace Authify.Application.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDictionary<TwoFactorMethod, ITwoFactorHandler> _handlers;
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public OtpService(IMemoryCache memoryCache, IEmailSender emailSender, ISmsSender smsSender, IDataProtectionProvider dataProtectionProvider)
    {
        _memoryCache = memoryCache;
        _dataProtectionProvider = dataProtectionProvider;
        
        // Handler Dictionary initialisieren
        _handlers = new Dictionary<TwoFactorMethod, ITwoFactorHandler>
        {
            { TwoFactorMethod.Email, new EmailTwoFactorHandler(emailSender) },
            { TwoFactorMethod.Sms, new SmsTwoFactorHandler(smsSender) }
        };
    }

    public async Task GenerateAndSendOtpAsync(string usernameOrDestination, TwoFactorMethod method)
    {
        var otp = new Random().Next(100000, 999999).ToString();
        _memoryCache.Set($"otp_{usernameOrDestination}", otp, _otpExpiration);

        if (_handlers.TryGetValue(method, out var handler))
        {
            await handler.SendOtpAsync(usernameOrDestination, otp);
        }
        else
        {
            throw new NotSupportedException($"TwoFactor method {method} is not supported.");
        }
    }

    public async Task<bool> ValidateOtpAsync(string usernameOrDestination, string otpCode)
    {
        if (_memoryCache.TryGetValue($"otp_{usernameOrDestination}", out string? cachedOtp))
        {
            if (cachedOtp == otpCode)
            {
                _memoryCache.Remove($"otp_{usernameOrDestination}");
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