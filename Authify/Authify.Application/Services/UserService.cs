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

    public UserService(UserManager<TUser> userManager, IEmailSender emailSender,
        InfrastructureOptions infrastructureOptions)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _infrastructureOptions = infrastructureOptions;
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
        
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var confirmationLink =
            $"{_infrastructureOptions.Domain}confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        await _emailSender.SendEmailAsync(user.Email!, "Confirm your email",
            $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.");

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

        await _emailSender.SendEmailAsync(user.Email!, "Reset your password",
            $"Reset your password by clicking <a href='{resetLink}'>here</a>.");

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