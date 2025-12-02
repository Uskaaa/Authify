using Authify.Core.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace Authify.Core.Interfaces;

public interface IOtpService<TUser>
{
    Task GenerateAndSendOtpAsync(TUser user, TwoFactorMethod method);
    Task<bool> ValidateOtpAsync(TUser user, TwoFactorMethod method, string otpCode);
    string GenerateToken(string email, bool rememberMe, TwoFactorMethod method);
    (string email, bool rememberMe, TwoFactorMethod method) ValidateToken(string token);
}