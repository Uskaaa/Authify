namespace Authify.Core.Interfaces;

public interface IOtpService
{
    Task GenerateAndSendOtpAsync(string email);
    Task<bool> ValidateOtpAsync(string email, string otpCode);
}