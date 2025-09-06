using System.Security.Claims;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Application.Services;

public class ExternalAuthServiceCookie<TUser> : IExternalAuthService
    where TUser : IdentityUser, new()
{
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;

    public ExternalAuthServiceCookie(SignInManager<TUser> signInManager, UserManager<TUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
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
            return new LocalRedirectResult(returnUrl ?? "/");

        // Sonst neuen User anlegen/verknüpfen
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return new RedirectResult("/login?error=email_claim_missing");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new TUser { UserName = email, Email = email };
            // Achtung: EmailConfirmed nur setzen, wenn Provider verifizierte Email liefert
            user.EmailConfirmed = true;
            var createRes = await _userManager.CreateAsync(user);
            if (!createRes.Succeeded)
                return new RedirectResult("/login?error=account_creation_failed");
        }

        var addLoginRes = await _userManager.AddLoginAsync(user, info);
        if (!addLoginRes.Succeeded)
            return new RedirectResult("/login?error=add_login_failed");

        await _signInManager.SignInAsync(user, isPersistent: false);
        return new LocalRedirectResult(returnUrl ?? "/");
    }
}