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
            return new RedirectResult($"{returnUrl}?error=external_login_failed");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return new RedirectResult($"{returnUrl}?error=external_login_info_null");



        // ================================================
        // MODE: CONNECT  →  KEIN USER DARF ERSTELLT WERDEN
        // ================================================
        if (mode == "connect")
        {
            string? userId = null;
            if (info.AuthenticationProperties?.Items.TryGetValue("LoginProviderUserId", out var id) == true)
            {
                userId = id;
            }

            if (string.IsNullOrEmpty(userId))
                return new RedirectResult($"{returnUrl}?error=user_context_lost");

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null)
                return new RedirectResult($"{returnUrl}?error=user_not_found");
            
            var externalEmail = info.Principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(externalEmail))
                return new RedirectResult($"{returnUrl}?error=external_email_missing");

            if (!string.Equals(currentUser.Email, externalEmail, StringComparison.OrdinalIgnoreCase))
            {
                return new RedirectResult($"{returnUrl}?error=email_mismatch");
            }

            var addLoginRes = await _userManager.AddLoginAsync(currentUser, info);
            if (!addLoginRes.Succeeded)
            {
                if (addLoginRes.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
                    return new RedirectResult($"{returnUrl}?error=provider_already_linked");
                
                return new RedirectResult($"{returnUrl}?error=provider_link_failed");
            }

            var redirectUrl = $"{returnUrl}?provider={info.LoginProvider}&key={info.ProviderKey}&display={info.ProviderDisplayName}";
            return new RedirectResult(redirectUrl);
        }



        // ==========================================================
        // NORMALER LOGIN / REGISTER FLOW (NICHT CONNECT)
        // ==========================================================
        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
                   ?? await CreateOrLinkUserAsync(info);



        // ======================
        // 2FA
        // ======================
        var preferredResult = await _twoFactorClaimService.GetPreferredAsync(user.Id);
        if (preferredResult.Success && preferredResult.Data != null)
        {
            var method = preferredResult.Data.Method;

            await _otpService.GenerateAndSendOtpAsync(user, method);
            var otpToken = _otpService.GenerateToken(user.Email, rememberMe: true, method);

            var redirectUrlOtp =
                $"/otp?token={otpToken}";
            return new RedirectResult(redirectUrlOtp);
        }



        // ======================
        // TOKEN GENERIEREN
        // ======================
        var jwt = await _jwtTokenService.GenerateTokenAsync(user.Id);

        var refresh =
            _jwtTokenService.GenerateRefreshToken(
                user.Id,
                deviceName: "WebApp",
                ipAddress: "unknown",
                rememberMe: true);

        await _context.RefreshTokens.AddAsync(refresh);
        await _context.SaveChangesAsync();

        var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        var redirectUrlFull =
            $"{target}#access={Uri.EscapeDataString(jwt)}&refresh={Uri.EscapeDataString(refresh.Token)}&remember=true";

        return new RedirectResult(redirectUrlFull);
    }



    // =======================================================
    //    User ERSTELLEN oder EXISTIERENDEN VERKNÜPFEN
    //    (ABER NUR für login/register; NICHT für connect!)
    // =======================================================
    private async Task<TUser> CreateOrLinkUserAsync(ExternalLoginInfo info)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("External login email missing.");

        var fullName = info.Principal.FindFirstValue(ClaimTypes.Name);
        var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
        var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            if (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(surname))
                fullName = $"{givenName} {surname}".Trim();
            else
                fullName = email.Split('@')[0];
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            existingUser = new TUser
            {
                UserName = email,
                FullName = fullName,
                Email = email,
                EmailConfirmed = true
            };

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