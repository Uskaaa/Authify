using System.Net;
using System.Net.Mail;
using Authify.Core.Interfaces;

namespace Authify.Application.Services;

public class EmailService : IEmailSender
{
    private readonly SmtpClient _smtpClient;
    private readonly string _senderEmail; 

    public EmailService(SmtpClient smtpClient)
    {
        _smtpClient = smtpClient;
        
        if (smtpClient.Credentials is NetworkCredential creds)
        {
            _senderEmail = creds.UserName;
        }
        else
        {
            _senderEmail = "noreply@mycelis.ai"; 
        }
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try 
        {
            var message = new MailMessage
            {
                From = new MailAddress(_senderEmail, "Mycelis Support"), 
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            
            message.To.Add(toEmail);

            await _smtpClient.SendMailAsync(message);
        }
        catch (SmtpException ex)
        {
            Console.WriteLine($"SMTP Fehler: {ex.Message} - Status: {ex.StatusCode}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Allgemeiner Email Fehler: {ex.Message}");
            throw;
        }
    }
}