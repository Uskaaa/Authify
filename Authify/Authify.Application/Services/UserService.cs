using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Extensions;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class UserService<TUser> : IUserService
    where TUser : ApplicationUser, new()
{
    private readonly UserManager<TUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly InfrastructureOptions _infrastructureOptions;
    private readonly IEnumerable<IUserRegistrationHook> _registrationHooks;

    public UserService(UserManager<TUser> userManager, IEmailSender emailSender,
        InfrastructureOptions infrastructureOptions, IEnumerable<IUserRegistrationHook> registrationHooks)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _infrastructureOptions = infrastructureOptions;
        _registrationHooks = registrationHooks;
    }

    public async Task<OperationResult> RegisterAsync(RegisterRequest request)
    {
        var user = new TUser
        {
            FullName = request.FullName,
            UserName = request.Email,
            Email = request.Email
        };

        var existingUser = await _userManager.FindByEmailAsync(user.Email);
        if (existingUser != null)
            return OperationResult.Fail("User already exists.");

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return OperationResult.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));

        foreach (var hook in _registrationHooks)
        {
            try
            {
                await hook.OnUserRegisteredAsync(user.Id, user.Email!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hook Error: {ex.Message}");
            }
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var confirmationLink =
            $"{_infrastructureOptions.Domain}confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', sans-serif; background-color: #f4f4f5; padding: 20px; margin: 0; }}
        .container {{ max-width: 500px; margin: 0 auto; background: #ffffff; padding: 40px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); }}
        .btn {{ display: inline-block; background-color: #4f46e5; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .text {{ color: #334155; line-height: 1.6; }}
        .link-fallback {{ font-size: 12px; color: #64748b; word-break: break-all; }}
    </style>
</head>
<body>
    <div class='container'>
        <h2 style='color: #1e293b; text-align: center; margin-top: 0;'>Confirm your email</h2>
        <p class='text'>Welcome to BrieflyAI!</p>
        <p class='text'>Please confirm your email address to get started.</p>
        <div style='text-align: center;'>
            <a href='{confirmationLink}' class='btn' style='color: #ffffff;'>Verify Email Address</a>
        </div>
        <p class='link-fallback'>Or click here: <a href='{confirmationLink}' style='color: #4f46e5;'>{confirmationLink}</a></p>
    </div>
</body>
</html>";

        await _emailSender.SendEmailAsync(user.Email!, "Confirm your email", htmlContent);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> ConfirmEmailAsync(EmailConfirmationRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);

        return result.Succeeded
            ? OperationResult.Ok()
            : OperationResult.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<OperationResult> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            return OperationResult.Fail("Invalid request.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink =
            $"{_infrastructureOptions.Domain}reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

        string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', sans-serif; background-color: #f4f4f5; padding: 20px; margin: 0; }}
        .container {{ max-width: 500px; margin: 0 auto; background: #ffffff; padding: 40px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); }}
        .btn {{ display: inline-block; background-color: #4f46e5; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .text {{ color: #334155; line-height: 1.6; }}
        .link-fallback {{ font-size: 12px; color: #64748b; word-break: break-all; }}
    </style>
</head>
<body>
    <div class='container'>
        <h2 style='color: #1e293b; text-align: center; margin-top: 0;'>Reset your password</h2>
        <p class='text'>Hello,</p>
        <p class='text'>We received a request to reset your password. Click the button below to choose a new one:</p>
        <div style='text-align: center;'>
            <a href='{resetLink}' class='btn' style='color: #ffffff;'>Reset Password</a>
        </div>
        <p class='text' style='font-size: 14px;'>If you didn't ask to reset your password, you can safely ignore this email.</p>
        <p class='link-fallback'>Or click here: <a href='{resetLink}' style='color: #4f46e5;'>{resetLink}</a></p>
    </div>
</body>
</html>";

        await _emailSender.SendEmailAsync(user.Email!, "Reset your password", htmlContent);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        return result.Succeeded
            ? OperationResult.Ok()
            : OperationResult.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<OperationResult> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        return result.Succeeded
            ? OperationResult.Ok()
            : OperationResult.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}