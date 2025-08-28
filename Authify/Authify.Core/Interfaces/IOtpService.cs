using Authify.Core.Models.Enums;

namespace Authify.Core.Interfaces;

public interface IOtpService
{
    Task GenerateAndSendOtpAsync(string usernameOrDestination, TwoFactorMethod method);
    Task<bool> ValidateOtpAsync(string usernameOrDestination, string otpCode);
    string GenerateToken(string email, bool rememberMe);
    (string email, bool rememberMe) ValidateToken(string token);
}