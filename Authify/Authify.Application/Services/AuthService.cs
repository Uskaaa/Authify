using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class AuthService : IAuthService
{
    private readonly IOtpService _otpService;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AuthService(IOtpService otpService, SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
        _otpService = otpService;
    }

    public async Task<OperationResult> LoginAsync(LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(request.UsernameOrEmail, request.Password,
            request.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            await _otpService.GenerateAndSendOtpAsync(request.UsernameOrEmail);
            return OperationResult.Ok();
        }

        return OperationResult.Fail("Invalid login attempt.");
    }

    public async Task<OperationResult> VerifyOtpAsync(OtpVerificationRequest request)
    {
        var isValid = await _otpService.ValidateOtpAsync(request.Email, request.OtpCode);

        if (!isValid)
            return OperationResult.Fail("Invalid OTP code.");

        return OperationResult.Ok();
    }
}