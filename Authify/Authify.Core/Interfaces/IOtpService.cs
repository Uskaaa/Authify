namespace Authify.Core.Interfaces;

public interface IOtpService
{
    Task GenerateAndSendOtpAsync(string email);
    Task<bool> ValidateOtpAsync(string email, string otpCode);
    string GenerateToken(string email, bool rememberMe);
    (string email, bool rememberMe) ValidateToken(string token);
}