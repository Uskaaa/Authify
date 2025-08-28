using System.Text.Json;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Models.Enums;
using Authify.Core.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class AuthService : IAuthService
{
    private readonly IOtpService _otpService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly TwoFactorClaimService _twoFactorClaimService;

    public AuthService(IOtpService otpService, SignInManager<IdentityUser> signInManager, TwoFactorClaimService twoFactorClaimService,
        UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _otpService = otpService;
        _userManager = userManager;
        _twoFactorClaimService = twoFactorClaimService;
    }

    public async Task<OperationResult<string>> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.UsernameOrEmail);
        if (user == null)
            return OperationResult<string>.Fail("Invalid login attempt.");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return OperationResult<string>.Fail("Please confirm your E-Mail first.");

        var resultCheckSignIn =
            await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        if (!resultCheckSignIn.Succeeded)
            return OperationResult<string>.Fail("Ungültige Anmeldedaten!");

        // 2FA prüfen
        var preferredResult = await _twoFactorClaimService.GetPreferredAsync(user.Id);
        if (preferredResult.Success && preferredResult.Data != null)
        {
            // Bevorzugte Methode auswählen
            var method = preferredResult.Data.Method;
            await _otpService.GenerateAndSendOtpAsync(request.UsernameOrEmail, method);
        }
        else
        {
            // Fallback: Standard-OTP (z.B. Email)
            await _otpService.GenerateAndSendOtpAsync(request.UsernameOrEmail, TwoFactorMethod.Email);
        }

        var token = _otpService.GenerateToken(request.UsernameOrEmail, request.RememberMe);

        return OperationResult<string>.Ok(token);
    }

    public async Task<OperationResult> VerifyOtpAsync(OtpVerificationRequest request)
    {
        var (email, rememberMe) = _otpService.ValidateToken(request.Token);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return OperationResult.Fail("Benutzer nicht gefunden.");

        var isValid = await _otpService.ValidateOtpAsync(email, request.OtpCode);
        if (!isValid)
            return OperationResult.Fail("Invalid OTP code.");

        await _signInManager.SignInAsync(user, isPersistent: rememberMe);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> ResendOtpAsync(ResendOtpRequest request)
    {
        // Token validieren (optional, abhängig von deiner Logik)
        var (email, rememberMe) = _otpService.ValidateToken(request.Token);

        if (!string.Equals(email, request.UsernameOrEmail, StringComparison.OrdinalIgnoreCase))
            return OperationResult.Fail("Token does not match user.");

        // OTP senden mit gewünschter Methode
        await _otpService.GenerateAndSendOtpAsync(request.UsernameOrEmail, request.TwoFactorMethod);

        return OperationResult.Ok();
    }
}