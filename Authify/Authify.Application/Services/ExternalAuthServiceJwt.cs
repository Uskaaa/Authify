using System.Security.Claims;
using Authify.Application.Data;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Application.Services;

public class ExternalAuthServiceJwt<TUser> : IExternalAuthService
    where TUser : IdentityUser, new()
{
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthifyDbContext _context;

    public ExternalAuthServiceJwt(
        SignInManager<TUser> signInManager,
        UserManager<TUser> userManager,
        IJwtTokenService jwtTokenService,
        IAuthifyDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
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

        // Versuchen, bestehenden Login zu verwenden
        var alreadyLinked = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        TUser user;

        if (alreadyLinked != null)
        {
            user = alreadyLinked;
        }
        else
        {
            // Neuen/Existierenden User anhand Email
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return new RedirectResult("/login?error=email_claim_missing");

            user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new TUser { UserName = email, Email = email, EmailConfirmed = true }; // ggf. EmailConfirmed nur bei verifizierter Email setzen
                var createRes = await _userManager.CreateAsync(user);
                if (!createRes.Succeeded)
                    return new RedirectResult("/login?error=account_creation_failed");
            }

            var addLoginRes = await _userManager.AddLoginAsync(user, info);
            if (!addLoginRes.Succeeded)
                return new RedirectResult("/login?error=add_login_failed");
        }

        // An dieser Stelle: KEIN Cookie-SignIn – wir liefern JWT + RefreshToken zurück
        var jwt = _jwtTokenService.GenerateToken(user);

        // Device/IP kannst du bei Bedarf über HttpContext abgreifen (Controller ruft Service, gibt IP/Agent mit)
        var device = "external";
        var ip = "unknown";
        var rememberMe = true; // optional: aus returnUrl/state lesen

        var refresh = _jwtTokenService.GenerateRefreshToken(user.Id, device, ip, rememberMe);
        await _context.RefreshTokens.AddAsync(refresh);
        await _context.SaveChangesAsync();

        // Redirect zur WASM-App mit Tokens im Fragment
        // Tipp: Nutze ein dediziertes Callback-Page (z.B. /auth/external-success), die die Tokens aus dem Fragment liest
        var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        var redirectUrl = $"{target}#access={Uri.EscapeDataString(jwt)}&refresh={Uri.EscapeDataString(refresh.Token)}&remember={(rememberMe ? "true" : "false")}";
        return new RedirectResult(redirectUrl);
    }
}