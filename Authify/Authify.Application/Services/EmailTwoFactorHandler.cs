using Authify.Application.Data;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class EmailTwoFactorHandler<TUser> : ITwoFactorHandler<TUser>
    where TUser : ApplicationUser
{
    private readonly IEmailSender _emailSender;

    public EmailTwoFactorHandler(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task SendOtpAsync(TUser user, string otp)
    {
        if (user.Email != null)
        {
            string htmlContent = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: 'Segoe UI', sans-serif; background-color: #f4f4f5; padding: 20px; margin: 0; }}
                .container {{ max-width: 500px; margin: 0 auto; background: #ffffff; padding: 40px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); }}
                .otp-code {{ font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #4f46e5; text-align: center; margin: 30px 0; background: #eef2ff; padding: 15px; border-radius: 8px; }}
                .text {{ color: #334155; line-height: 1.6; }}
                .footer {{ font-size: 12px; color: #94a3b8; text-align: center; margin-top: 30px; border-top: 1px solid #e2e8f0; padding-top: 20px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h2 style='color: #1e293b; text-align: center; margin-top: 0;'>Your OTP Code</h2>
                <p class='text'>Hello,</p>
                <p class='text'>Use the code below to complete your login:</p>
                <div class='otp-code'>{otp}</div>
                <p class='text' style='font-size: 14px;'>This code is valid for 10 minutes.</p>
                <div class='footer'>&copy; BrieflyAI Security</div>
            </div>
        </body>
        </html>";

            await _emailSender.SendEmailAsync(user.Email, "Your OTP Code", htmlContent);
        }
    }
}