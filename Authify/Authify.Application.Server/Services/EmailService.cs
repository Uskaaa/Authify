using System.Net.Mail;
using Authify.Core.Interfaces;

namespace Authify.Application.Services;

public class EmailService : IEmailSender
{
    private readonly SmtpClient _smtpClient;

    public EmailService(SmtpClient smtpClient)
    {
        _smtpClient = smtpClient;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MailMessage
        {
            To = { toEmail },
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        await _smtpClient.SendMailAsync(message);
    }
}