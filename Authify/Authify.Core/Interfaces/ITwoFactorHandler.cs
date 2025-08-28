namespace Authify.Core.Interfaces;

public interface ITwoFactorHandler
{
    Task SendOtpAsync(string destination, string otp);
}
