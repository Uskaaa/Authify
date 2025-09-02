using Authify.Application.Services;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Models.Enums;
using Authify.Core.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class CookieAuthService<TUser> : IAuthServiceCookie
    where TUser : IdentityUser
{
    private readonly IOtpService _otpService;
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;
    private readonly TwoFactorClaimService<TUser> _twoFactorClaimService;
    private readonly IUserAccountService _userAccountService;

    public CookieAuthService(IOtpService otpService, SignInManager<TUser> signInManager,
        TwoFactorClaimService<TUser> twoFactorClaimService,
        UserManager<TUser> userManager,
        IUserAccountService userAccountService)
    {
        _signInManager = signInManager;
        _otpService = otpService;
        _userManager = userManager;
        _twoFactorClaimService = twoFactorClaimService;
        _userAccountService = userAccountService;
    }

    public async Task<OperationResult<string>> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.UsernameOrEmail);
        if (user == null)
            return OperationResult<string>.Fail("Invalid login attempt.");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return OperationResult<string>.Fail("Please confirm your E-Mail first.");

        var deactivation = await _userAccountService.GetDeactivationStatusAsync(user.Id);
        if (deactivation.Data != null && deactivation.Data.IsDeactivated)
            return OperationResult<string>.Fail("Your account is deactivated. Please contact support!");

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

            var token = _otpService.GenerateToken(request.UsernameOrEmail, request.RememberMe);
            return OperationResult<string>.Ok(token);
        }

        await _signInManager.SignInAsync(user, isPersistent: request.RememberMe);
        return OperationResult<string>.Ok(null!);
    }

    public async Task<OperationResult<string>> VerifyOtpAsync(OtpVerificationRequest request)
    {
        var (email, rememberMe) = _otpService.ValidateToken(request.Token);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return OperationResult<string>.Fail("Benutzer nicht gefunden.");

        var isValid = await _otpService.ValidateOtpAsync(email, request.OtpCode);
        if (!isValid)
            return OperationResult<string>.Fail("Invalid OTP code.");

        await _signInManager.SignInAsync(user, isPersistent: rememberMe);

        return OperationResult<string>.Ok(null!);
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

    public async Task<OperationResult> LogoutAsync()
    {
        try
        {
            await _signInManager.SignOutAsync(); // beendet die Session und löscht das Auth-Cookie
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Logout failed: {ex.Message}");
        }
    }
}