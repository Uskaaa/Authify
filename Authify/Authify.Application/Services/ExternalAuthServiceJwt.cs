using System.Security.Claims;
using Authify.Application.Data;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Application.Services;

public class ExternalAuthServiceJwt<TUser> : IExternalAuthService
    where TUser : ApplicationUser, new()
{
    private readonly SignInManager<TUser> _signInManager;
    private readonly UserManager<TUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtpService<TUser> _otpService;
    private readonly ITwoFactorClaimService _twoFactorClaimService;
    private readonly IAuthifyDbContext _context;

    public ExternalAuthServiceJwt(
        SignInManager<TUser> signInManager,
        UserManager<TUser> userManager,
        IJwtTokenService jwtTokenService,
        IOtpService<TUser> otpService,
        ITwoFactorClaimService twoFactorClaimService,
        IAuthifyDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _otpService = otpService;
        _twoFactorClaimService = twoFactorClaimService;
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

        TUser user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
                     ?? await CreateOrLinkUserAsync(info);

        // ---- 2FA prüfen ----
        var preferredResult = await _twoFactorClaimService.GetPreferredAsync(user.Id);
        if (preferredResult.Success && preferredResult.Data != null)
        {
            var method = preferredResult.Data.Method;
            await _otpService.GenerateAndSendOtpAsync(user, method);

            var otpToken = _otpService.GenerateToken(user.Email, rememberMe: true, method);
            var redirectUrlOtp =
                $"/twofactor?token={Uri.EscapeDataString(otpToken)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";
            return new RedirectResult(redirectUrlOtp);
        }

        // ---- Kein 2FA ----
        var jwt = await _jwtTokenService.GenerateTokenAsync(user.Id);
        var refresh =
            _jwtTokenService.GenerateRefreshToken(user.Id, deviceName: "external", ipAddress: "unknown",
                rememberMe: true);

        await _context.RefreshTokens.AddAsync(refresh);
        await _context.SaveChangesAsync();

        var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        var redirectUrl =
            $"{target}#access={Uri.EscapeDataString(jwt)}&refresh={Uri.EscapeDataString(refresh.Token)}&remember=true";
        return new RedirectResult(redirectUrl);
    }

    private async Task<TUser> CreateOrLinkUserAsync(ExternalLoginInfo info)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("External login email missing.");

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            existingUser = new TUser { UserName = email, Email = email, EmailConfirmed = true };
            var createRes = await _userManager.CreateAsync(existingUser);
            if (!createRes.Succeeded)
                throw new Exception("Account creation failed.");
        }

        var addLoginRes = await _userManager.AddLoginAsync(existingUser, info);
        if (!addLoginRes.Succeeded)
            throw new Exception("Failed to link external login.");

        return existingUser;
    }
}