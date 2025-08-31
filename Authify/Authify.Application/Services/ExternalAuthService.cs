using System.Security.Claims;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Application.Services;

public class ExternalAuthService<TUser> : IExternalAuthService
    where TUser : IdentityUser, new()
{
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;

    public ExternalAuthService(SignInManager<TUser> signInManager, UserManager<TUser> userManager)
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
        if (remoteError != null)
            return new RedirectResult("/login?error=external_login_failed");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return new RedirectResult("/login?error=external_login_info_null");

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
        if (result.Succeeded)
            return new LocalRedirectResult(returnUrl ?? "/");

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email == null)
            return new RedirectResult("/login?error=email_claim_missing");

        var user = new TUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
            return new RedirectResult("/login?error=account_creation_failed");

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, false);

        return new LocalRedirectResult(returnUrl ?? "/");
    }
}