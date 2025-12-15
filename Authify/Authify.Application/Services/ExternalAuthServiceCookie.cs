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

    public AuthenticationProperties GetAuthProperties(string provider, string redirectUrl, string? userId = null)
    {
        var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        
        if (!string.IsNullOrEmpty(userId))
        {
            props.Items["LoginProviderUserId"] = userId;
        }
        
        return props;
    }

    public string GetRedirectUrl(string provider, string? returnUrl, string mode)
        => $"/auth/externallogin-callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}&mode={mode}";

    public async Task<IActionResult> HandleExternalCallbackAsync(string? returnUrl, string mode, string? remoteError)
    {
        if (!string.IsNullOrEmpty(remoteError))
            return new RedirectResult("/login?error=external_login_failed");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return new RedirectResult("/login?error=external_login_info_null");

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return new RedirectResult("/login?error=email_claim_missing");

        // full name extraction
        var fullName = info.Principal.FindFirstValue(ClaimTypes.Name)
                      ?? $"{info.Principal.FindFirstValue(ClaimTypes.GivenName)} {info.Principal.FindFirstValue(ClaimTypes.Surname)}".Trim();

        if (string.IsNullOrWhiteSpace(fullName))
            fullName = email.Split('@')[0];

        // -------------------------------
        // CONNECT MODE  → nur verlinken
        // -------------------------------
        if (mode == "connect")
        {
            // Frontend bekommt provider + key zurück
            var redirectUrl =
                $"{returnUrl}#provider={info.LoginProvider}&key={info.ProviderKey}&display={info.ProviderDisplayName}";

            return new RedirectResult(redirectUrl);
        }

        // -------------------------------
        // LOGIN MODE → Wenn Login existiert
        // -------------------------------
        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false);

        if (signInResult.Succeeded)
        {
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            // 2FA
            var pref = await _twoFactorClaimService.GetPreferredAsync(user.Id);
            if (pref.Success && pref.Data != null)
            {
                var method = pref.Data.Method;
                await _otpService.GenerateAndSendOtpAsync(user, method);

                var token = _otpService.GenerateToken(user.Email, false, method);
                return new RedirectResult($"/twofactor?token={token}");
            }

            // direkt einloggen
            return new LocalRedirectResult(returnUrl ?? "/");
        }

        // -------------------------------
        // REGISTRATION MODE → Account anlegen
        // -------------------------------
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser == null)
        {
            existingUser = new TUser
            {
                Email = email,
                UserName = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var create = await _userManager.CreateAsync(existingUser);
            if (!create.Succeeded)
                return new RedirectResult("/login?error=account_creation_failed");
        }

        // Login verknüpfen für User
        var add = await _userManager.AddLoginAsync(existingUser, info);
        if (!add.Succeeded)
            return new RedirectResult("/login?error=add_login_failed");

        // 2FA
        var prefNew = await _twoFactorClaimService.GetPreferredAsync(existingUser.Id);
        if (prefNew.Success && prefNew.Data != null)
        {
            var method = prefNew.Data.Method;
            await _otpService.GenerateAndSendOtpAsync(existingUser, method);

            var token = _otpService.GenerateToken(existingUser.Email, false, method);
            return new RedirectResult($"/twofactor?token={token}");
        }

        // Normale Cookie-SignIn
        await _signInManager.SignInAsync(existingUser, false);
        return new LocalRedirectResult(returnUrl ?? "/");
    }
}