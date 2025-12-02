using System.Security.Claims;
using Authify.Application.Data;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Application.Services;

public class ExternalAuthServiceCookie<TUser> : IExternalAuthService
    where TUser : ApplicationUser, new()
{
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;
    private readonly ITwoFactorClaimService _twoFactorClaimService;
    private readonly IOtpService<TUser> _otpService;

    public ExternalAuthServiceCookie(
        SignInManager<TUser> signInManager,
        UserManager<TUser> userManager,
        ITwoFactorClaimService twoFactorClaimService,
        IOtpService<TUser> otpService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _twoFactorClaimService = twoFactorClaimService;
        _otpService = otpService;
    }

    public AuthenticationProperties GetAuthProperties(string provider, string redirectUrl)
        => _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

    public string GetRedirectUrl(string provider, string? returnUrl)
        => $"/auth/externallogin-callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";

    public async Task<IActionResult> HandleExternalCallbackAsync(string? returnUrl, string? remoteError)
    {
        if (!string.IsNullOrEmpty(remoteError))
            return new RedirectResult("/login?error=external_login_failed");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return new RedirectResult("/login?error=external_login_info_null");

        // Bereits verknüpfter Login?
        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
        if (signInResult.Succeeded)
        {
            // 2FA prüfen
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                var preferredResult = await _twoFactorClaimService.GetPreferredAsync(user.Id);
                if (preferredResult.Success && preferredResult.Data != null)
                {
                    var method = preferredResult.Data.Method;
                    await _otpService.GenerateAndSendOtpAsync(user, method);

                    var token = _otpService.GenerateToken(user.Email, false, preferredResult.Data.Method);
                    // Hier könnte man ein spezielles ViewModel oder Redirect mit Token zurückgeben
                    return new RedirectResult($"/twofactor?token={token}");
                }
            }

            return new LocalRedirectResult(returnUrl ?? "/");
        }

        // Sonst neuen User anlegen/verknüpfen
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return new RedirectResult("/login?error=email_claim_missing");

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            existingUser = new TUser { UserName = email, Email = email, EmailConfirmed = true };
            var createRes = await _userManager.CreateAsync(existingUser);
            if (!createRes.Succeeded)
                return new RedirectResult("/login?error=account_creation_failed");
        }

        var addLoginRes = await _userManager.AddLoginAsync(existingUser, info);
        if (!addLoginRes.Succeeded)
            return new RedirectResult("/login?error=add_login_failed");

        // 2FA prüfen
        var preferredResultNewUser = await _twoFactorClaimService.GetPreferredAsync(existingUser.Id);
        if (preferredResultNewUser.Success && preferredResultNewUser.Data != null)
        {
            var method = preferredResultNewUser.Data.Method;
            await _otpService.GenerateAndSendOtpAsync(existingUser, method);

            var token = _otpService.GenerateToken(existingUser.Email, false, preferredResultNewUser.Data.Method);
            return new RedirectResult($"/twofactor?token={token}");
        }

        await _signInManager.SignInAsync(existingUser, isPersistent: false);
        return new LocalRedirectResult(returnUrl ?? "/");
    }
}