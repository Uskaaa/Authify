using System.Text.Json;
using Authify.Application.Data;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Models.Enums;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace Authify.Application.Services;

public class OtpService<TUser> : IOtpService<TUser>
    where TUser : ApplicationUser
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDictionary<TwoFactorMethod, ITwoFactorHandler<TUser>> _handlers;
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(5);
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly UserManager<TUser> _userManager;

    public OtpService(IMemoryCache memoryCache, IEmailSender emailSender, ISmsSender smsSender,
        IDataProtectionProvider dataProtectionProvider, UserManager<TUser> userManager)
    {
        _memoryCache = memoryCache;
        _dataProtectionProvider = dataProtectionProvider;
        _userManager = userManager;

        // Handler Dictionary initialisieren
        _handlers = new Dictionary<TwoFactorMethod, ITwoFactorHandler<TUser>>
        {
            { TwoFactorMethod.Email, new EmailTwoFactorHandler<TUser>(emailSender) },
            { TwoFactorMethod.Sms, new SmsTwoFactorHandler<TUser>(smsSender) }
        };
    }

    public async Task GenerateAndSendOtpAsync(TUser user, TwoFactorMethod method)
    {
        // Authenticator-app codes are generated client-side from the shared secret already on
        // the user's device — there is nothing for the server to generate or send here.
        if (method == TwoFactorMethod.Totp) return;

        var otp = new Random().Next(100000, 999999).ToString();
        _memoryCache.Set($"otp_{user.Id}_{method}", otp, _otpExpiration);

        if (_handlers.TryGetValue(method, out var handler))
        {
            await handler.SendOtpAsync(user, otp);
        }
        else
        {
            throw new NotSupportedException($"TwoFactor method {method} is not supported.");
        }
    }

    public async Task<bool> ValidateOtpAsync(TUser user, TwoFactorMethod method, string otpCode)
    {
        if (method == TwoFactorMethod.Totp)
        {
            var normalizedCode = otpCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            return await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, normalizedCode);
        }

        if (_memoryCache.TryGetValue($"otp_{user.Id}_{method}", out string? cachedOtp))
        {
            if (cachedOtp == otpCode)
            {
                _memoryCache.Remove($"otp_{user.Id}_{method}");
                return true;
            }
        }

        return false;
    }

    // Methode zur Tokenverschlüsselung
    public string GenerateToken(string email, bool rememberMe, TwoFactorMethod method)
    {
        OtpPayload otpPayload = new OtpPayload
        {
            Email = email,
            RememberMe = rememberMe,
            Method = method
        };
        var payload = JsonSerializer.Serialize(otpPayload);
        var protector = _dataProtectionProvider.CreateProtector("otp-auth");
        return protector.Protect(payload);
    }

    // Methode zur Entschlüsselung eines Tokens
    public (string email, bool rememberMe, TwoFactorMethod method) ValidateToken(string token)
    {
        var protector = _dataProtectionProvider.CreateProtector("otp-auth");
        var decryptedData = protector.Unprotect(token);
        var payload = JsonSerializer.Deserialize<OtpPayload>(decryptedData);

        return (payload.Email, payload.RememberMe, payload.Method);
    }
}