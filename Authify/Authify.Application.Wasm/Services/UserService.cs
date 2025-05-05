using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailSender _emailSender;

    public UserService(UserManager<IdentityUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    public async Task<OperationResult> RegisterAsync(RegisterRequest request)
    {
        var user = new IdentityUser
        {
            UserName = request.Username,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return OperationResult.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var confirmationLink = $"https://your-app.com/api/User/confirmemail?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        await _emailSender.SendEmailAsync(user.Email!, "Confirm your email", $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.");

        return OperationResult.Ok();
    }

    public async Task<OperationResult> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var result = await _userManager.ConfirmEmailAsync(user, token);

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
        var resetLink = $"https://your-app.com/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

        await _emailSender.SendEmailAsync(user.Email!, "Reset your password", $"Reset your password by clicking <a href='{resetLink}'>here</a>.");

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
}