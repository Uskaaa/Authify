using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class JwtAuthService<TUser> : IAuthServiceJwt
    where TUser : ApplicationUser
{
    private readonly IOtpService<TUser> _otpService;
    private readonly SignInManager<TUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly UserManager<TUser> _userManager;
    private readonly ITwoFactorClaimService _twoFactorClaimService;
    private readonly IUserAccountService _userAccountService;
    private readonly IAuthifyDbContext _context;
    private readonly ILdapService _ldapService;

    public JwtAuthService(IOtpService<TUser> otpService, SignInManager<TUser> signInManager, IJwtTokenService jwtTokenService,
        ITwoFactorClaimService twoFactorClaimService,
        UserManager<TUser> userManager,
        IUserAccountService userAccountService,
        IAuthifyDbContext context,
        ILdapService ldapService)
    {
        _jwtTokenService = jwtTokenService;
        _signInManager = signInManager;
        _otpService = otpService;
        _userManager = userManager;
        _twoFactorClaimService = twoFactorClaimService;
        _userAccountService = userAccountService;
        _context = context;
        _ldapService = ldapService;
    }

    public async Task<OperationResult<(string AccessToken, string RefreshToken)?>> LoginAsync(LoginRequest request)
    {
        // ── LDAP-Intercept ───────────────────────────────────────────────────
        var domain = ExtractDomain(request.UsernameOrEmail);
        if (domain != null)
        {
            var ldapConfig = await _ldapService.GetConfigurationForDomainAsync(domain);
            if (ldapConfig != null)
            {
                var (ldapSuccess, displayName, ldapError) =
                    await _ldapService.AuthenticateAsync(request.UsernameOrEmail, request.Password, ldapConfig);

                if (!ldapSuccess)
                    return OperationResult<(string, string)?>.Fail(ldapError ?? "Ungültige Anmeldedaten!");

                var provisionResult = await _ldapService.EnsureUserProvisionedAsync(
                    request.UsernameOrEmail, displayName, ldapConfig);
                if (!provisionResult.Success)
                    return OperationResult<(string, string)?>.Fail(provisionResult.ErrorMessage!);

                var ldapUser = await _userManager.FindByIdAsync(provisionResult.Data!);
                if (ldapUser == null)
                    return OperationResult<(string, string)?>.Fail("LDAP-Benutzer konnte nicht geladen werden.");

                var deactivationLdap = await _userAccountService.GetDeactivationStatusAsync(ldapUser.Id);
                if (deactivationLdap.Data != null && deactivationLdap.Data.IsDeactivated)
                    return OperationResult<(string, string)?>.Fail("Your account is deactivated. Please contact support!");

                return await BuildTokenResultAsync(ldapUser, request);
            }
        }

        // ── Normaler Identity-Login ───────────────────────────────────────────
        var user = await _userManager.FindByEmailAsync(request.UsernameOrEmail);
        if (user == null)
            return OperationResult<(string, string)?>.Fail("Invalid login attempt.");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return OperationResult<(string, string)?>.Fail("Please confirm your E-Mail first.");

        var deactivation = await _userAccountService.GetDeactivationStatusAsync(user.Id);
        if (deactivation.Data != null && deactivation.Data.IsDeactivated)
            return OperationResult<(string, string)?>.Fail("Your account is deactivated. Please contact support!");

        var resultCheckSignIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (resultCheckSignIn.IsLockedOut)
            return OperationResult<(string, string)?>.Fail("Account ist wegen zu vieler Fehlversuche gesperrt. Bitte warten Sie 5 Minuten.");
        if (!resultCheckSignIn.Succeeded)
            return OperationResult<(string, string)?>.Fail("Ungültige Anmeldedaten.");

        return await BuildTokenResultAsync(user, request);
    }

    public async Task<OperationResult<(string AccessToken, string RefreshToken)?>> VerifyOtpAsync(OtpVerificationRequest request)
    {
        var (email, rememberMe, twoFactorMethod) = _otpService.ValidateToken(request.Token);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return OperationResult<(string, string)?>.Fail("Benutzer nicht gefunden.");

        var isValid = await _otpService.ValidateOtpAsync(user, twoFactorMethod, request.OtpCode);
        if (!isValid)
            return OperationResult<(string, string)?>.Fail("Invalid OTP code.");

        // JWT + RefreshToken erstellen
        var jwtToken = await _jwtTokenService.GenerateTokenAsync(user.Id);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, request.DeviceName, request.IpAddress ?? "Unknown", rememberMe);

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return OperationResult<(string, string)?>.Ok((jwtToken, refreshToken.Token));
    }

    public async Task<OperationResult> ResendOtpAsync(ResendOtpRequest request)
    {
        // Token validieren (optional, abhängig von deiner Logik)
        var (email, rememberMe, twoFactorMethod) = _otpService.ValidateToken(request.Token);

        if (!string.Equals(email, request.UsernameOrEmail, StringComparison.OrdinalIgnoreCase))
            return OperationResult.Fail("Token does not match user.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return OperationResult<(string, string)?>.Fail("Benutzer nicht gefunden.");
        
        // OTP senden mit gewünschter Methode
        await _otpService.GenerateAndSendOtpAsync(user, request.TwoFactorMethod);

        return OperationResult.Ok();
    }
    
    public async Task LogoutAsync(string refreshToken)
    {
        var rt = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (rt != null)
        {
            rt.IsRevoked = true;
            await _context.SaveChangesAsync();
        }
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private async Task<OperationResult<(string AccessToken, string RefreshToken)?>> BuildTokenResultAsync(
        TUser user, LoginRequest request)
    {
        var preferredResult = await _twoFactorClaimService.GetPreferredAsync(user.Id);
        if (preferredResult.Success && preferredResult.Data != null)
        {
            var method = preferredResult.Data.Method;
            await _otpService.GenerateAndSendOtpAsync(user, method);
            var otpToken = _otpService.GenerateToken(user.Email!, request.RememberMe, method);
            return OperationResult<(string, string)?>.Ok((AccessToken: otpToken, RefreshToken: string.Empty));
        }

        var jwtToken = await _jwtTokenService.GenerateTokenAsync(user.Id);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, request.DeviceName, request.IpAddress, request.RememberMe);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
        return OperationResult<(string, string)?>.Ok((jwtToken, refreshToken.Token));
    }

    private static string? ExtractDomain(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[(atIndex + 1)..].ToLowerInvariant() : null;
    }
}