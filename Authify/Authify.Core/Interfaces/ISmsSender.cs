namespace Authify.Core.Interfaces;

public interface ISmsSender
{
    Task SendSmsAsync(string destination, string otp);
}