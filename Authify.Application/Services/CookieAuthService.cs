using Authify.Application.Data;
using Authify.Application.Services;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Services;

public class CookieAuthService<TUser> : IAuthServiceCookie
    where TUser : ApplicationUser
{
    private readonly IOtpService<TUser> _otpService;
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;
    private readonly ITwoFactorClaimService _twoFactorClaimService;
    private readonly IUserAccountService _userAccountService;
    private readonly ILdapService _ldapService;

    public CookieAuthService(IOtpService<TUser> otpService, SignInManager<TUser> signInManager,
        ITwoFactorClaimService twoFactorClaimService,
        UserManager<TUser> userManager,
        IUserAccountService userAccountService,
        ILdapService ldapService)
    {
        _signInManager = signInManager;
        _otpService = otpService;
        _userManager = userManager;
        _twoFactorClaimService = twoFactorClaimService;
        _userAccountService = userAccountService;
        _ldapService = ldapService;
    }

    // ---- Core validation logic (no SignInAsync) ----

    public async Task<OperationResult<PendingLoginResult>> ValidateCredentialsAsync(LoginRequest request)
    {
        // ── LDAP-Intercept: Domain prüfen bevor Identity-Lookup ──────────────
        var domain = ExtractDomain(request.UsernameOrEmail);
        if (domain != null)
        {
            var ldapConfig = await _ldapService.GetConfigurationForDomainAsync(domain);
            if (ldapConfig != null)
            {
                var (ldapSuccess, displayName, ldapError) =
                    await _ldapService.AuthenticateAsync(request.UsernameOrEmail, request.Password, ldapConfig);

                if (!ldapSuccess)
                    return OperationResult<PendingLoginResult>.Fail(ldapError ?? "Ungültige Anmeldedaten!");

                var provisionResult = await _ldapService.EnsureUserProvisionedAsync(
                    request.UsernameOrEmail, displayName, ldapConfig);
                if (!provisionResult.Success)
                    return OperationResult<PendingLoginResult>.Fail(provisionResult.ErrorMessage!);

                var ldapUser = await _userManager.FindByIdAsync(provisionResult.Data!);
                if (ldapUser == null)
                    return OperationResult<PendingLoginResult>.Fail("LDAP-Benutzer konnte nicht geladen werden.");

                var deactivationLdap = await _userAccountService.GetDeactivationStatusAsync(ldapUser.Id);
                if (deactivationLdap.Data != null && deactivationLdap.Data.IsDeactivated)
                    return OperationResult<PendingLoginResult>.Fail("Ihr Konto ist deaktiviert. Bitte wenden Sie sich an den Support.");

                return await BuildPendingResultAsync(ldapUser, request.RememberMe);
            }
        }

        // ── Normaler Identity-Login ───────────────────────────────────────────
        var user = await _userManager.FindByEmailAsync(request.UsernameOrEmail);
        if (user == null)
            return OperationResult<PendingLoginResult>.Fail("Invalid login attempt.");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return OperationResult<PendingLoginResult>.Fail("Please confirm your E-Mail first.");

        var deactivation = await _userAccountService.GetDeactivationStatusAsync(user.Id);
        if (deactivation.Data != null && deactivation.Data.IsDeactivated)
            return OperationResult<PendingLoginResult>.Fail("Your account is deactivated. Please contact support!");

        var checkResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!checkResult.Succeeded)
            return OperationResult<PendingLoginResult>.Fail("Ungültige Anmeldedaten!");

        return await BuildPendingResultAsync(user, request.RememberMe);
    }

    public async Task<OperationResult<PendingLoginResult>> ValidateOtpAsync(OtpVerificationRequest request)
    {
        var (email, rememberMe, method) = _otpService.ValidateToken(request.Token);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return OperationResult<PendingLoginResult>.Fail("Benutzer nicht gefunden.");

        var isValid = await _otpService.ValidateOtpAsync(user, method, request.OtpCode);
        if (!isValid)
            return OperationResult<PendingLoginResult>.Fail("Invalid OTP code.");

        return OperationResult<PendingLoginResult>.Ok(new PendingLoginResult
        {
            UserId = user.Id,
            IsPersistent = rememberMe
        });
    }

    public async Task<OperationResult> CompleteSignInAsync(string userId, bool isPersistent)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        await _signInManager.SignInAsync(user, isPersistent: isPersistent);
        return OperationResult.Ok();
    }

    // ---- Legacy methods (kept for backward compatibility) ----

    [System.Obsolete("Use ValidateCredentialsAsync + CompleteSignInAsync for the HTTP redirect pattern.")]
    public async Task<OperationResult<string>> LoginAsync(LoginRequest request)
    {
        var pending = await ValidateCredentialsAsync(request);
        if (!pending.Success)
            return OperationResult<string>.Fail(pending.ErrorMessage!);

        if (pending.Data!.RequiresOtp)
            return OperationResult<string>.Ok(pending.Data.OtpToken!);

        await _signInManager.SignInAsync(
            (await _userManager.FindByIdAsync(pending.Data.UserId!))!,
            isPersistent: pending.Data.IsPersistent);

        return OperationResult<string>.Ok(null!);
    }

    [System.Obsolete("Use ValidateOtpAsync + CompleteSignInAsync for the HTTP redirect pattern.")]
    public async Task<OperationResult<string>> VerifyOtpAsync(OtpVerificationRequest request)
    {
        var pending = await ValidateOtpAsync(request);
        if (!pending.Success)
            return OperationResult<string>.Fail(pending.ErrorMessage!);

        await _signInManager.SignInAsync(
            (await _userManager.FindByIdAsync(pending.Data!.UserId!))!,
            isPersistent: pending.Data.IsPersistent);

        return OperationResult<string>.Ok(null!);
    }

    public async Task<OperationResult> ResendOtpAsync(ResendOtpRequest request)
    {
        var (email, rememberMe, method) = _otpService.ValidateToken(request.Token);

        if (!string.Equals(email, request.UsernameOrEmail, StringComparison.OrdinalIgnoreCase))
            return OperationResult.Fail("Token does not match user.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return OperationResult.Fail("Benutzer nicht gefunden.");

        await _otpService.GenerateAndSendOtpAsync(user, request.TwoFactorMethod);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> LogoutAsync()
    {
        try
        {
            await _signInManager.SignOutAsync();
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Logout failed: {ex.Message}");
        }
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private async Task<OperationResult<PendingLoginResult>> BuildPendingResultAsync(TUser user, bool rememberMe)
    {
        var preferredResult = await _twoFactorClaimService.GetPreferredAsync(user.Id);
        if (preferredResult.Success && preferredResult.Data != null)
        {
            var method = preferredResult.Data.Method;
            await _otpService.GenerateAndSendOtpAsync(user, method);
            var otpToken = _otpService.GenerateToken(user.Email!, rememberMe, method);
            return OperationResult<PendingLoginResult>.Ok(new PendingLoginResult
            {
                RequiresOtp = true,
                OtpToken = otpToken
            });
        }

        return OperationResult<PendingLoginResult>.Ok(new PendingLoginResult
        {
            UserId = user.Id,
            IsPersistent = rememberMe
        });
    }

    private static string? ExtractDomain(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[(atIndex + 1)..].ToLowerInvariant() : null;
    }
}