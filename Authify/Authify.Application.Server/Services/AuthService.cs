using System.Text.Json;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Server.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;

namespace Authify.Application.Services;

public class AuthService : IAuthService
{
    private readonly IOtpService _otpService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AuthService(IOtpService otpService, SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _otpService = otpService;
        _userManager = userManager;
    }

    public async Task<OperationResult<string>> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.UsernameOrEmail);
        if (user != null)
        {
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return OperationResult<string>.Fail("Please confirm your E-Mail first.");
            }

            var resultCheckSignIn =
                await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

            if (!resultCheckSignIn.Succeeded) return OperationResult<string>.Fail("Ungültige Anmeldedaten!");

            await _otpService.GenerateAndSendOtpAsync(request.UsernameOrEmail);

            var token = _otpService.GenerateToken(request.UsernameOrEmail, request.RememberMe);
            
            return OperationResult<string>.Ok(token);
        }

        return OperationResult<string>.Fail("Invalid login attempt.");
    }

    public async Task<OperationResult> VerifyOtpAsync(OtpVerificationRequest request)
    {
        var (email, rememberMe) = _otpService.ValidateToken(request.Token);

        var user = await _userManager.FindByIdAsync(email);
        if (user == null)
            return OperationResult.Fail("Benutzer nicht gefunden.");

        var isValid = await _otpService.ValidateOtpAsync(email, request.OtpCode);
        if (!isValid)
            return OperationResult.Fail("Invalid OTP code.");

        await _signInManager.SignInAsync(user, isPersistent: rememberMe);

        return OperationResult.Ok();
    }
}