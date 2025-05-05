using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[Route("auth")]
public class ExternalAuthController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public ExternalAuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("login/{provider}")]
    public IActionResult ExternalLogin(string provider, string? returnUrl = "/")
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "ExternalAuth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("externallogin-callback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = "/", string? remoteError = null)
    {
        if (remoteError != null)
        {
            // Log error here if needed
            return Redirect("/login?error=external_login_failed");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return Redirect("/login?error=external_login_info_null");
        }

        var result =
            await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl!);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email == null)
        {
            return Redirect("/login?error=email_claim_missing");
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true // Da von OAuth verifiziert
        };

        var identityResult = await _userManager.CreateAsync(user);
        if (!identityResult.Succeeded)
        {
            return Redirect("/login?error=account_creation_failed");
        }

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return LocalRedirect(returnUrl!);
    }
}