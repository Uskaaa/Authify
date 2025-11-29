using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class JwtAuthService<TUser> : IAuthServiceJwt
    where TUser : IdentityUser
{
    private readonly IOtpService<TUser> _otpService;
    private readonly SignInManager<TUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly UserManager<TUser> _userManager;
    private readonly ITwoFactorClaimService _twoFactorClaimService;
    private readonly IUserAccountService _userAccountService;
    private readonly IAuthifyDbContext _context;

    public JwtAuthService(IOtpService<TUser> otpService, SignInManager<TUser> signInManager, IJwtTokenService jwtTokenService,
        ITwoFactorClaimService twoFactorClaimService,
        UserManager<TUser> userManager,
        IUserAccountService userAccountService,
        IAuthifyDbContext context)
    {
        _jwtTokenService = jwtTokenService;
        _signInManager = signInManager;
        _otpService = otpService;
        _userManager = userManager;
        _twoFactorClaimService = twoFactorClaimService;
        _userAccountService = userAccountService;
        _context = context;
    }

    public async Task<OperationResult<(string AccessToken, string RefreshToken)?>> LoginAsync(LoginRequest request)
    {
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
        {
            return OperationResult<(string, string)?>.Fail("Account ist wegen zu vieler Fehlversuche gesperrt. Bitte warten Sie 5 Minuten.");
        }
        if (!resultCheckSignIn.Succeeded)
        {
            return OperationResult<(string, string)?>.Fail("Ungültige Anmeldedaten.");
        }
        
        // ---- 2FA prüfen ----
        var preferredResult = await _twoFactorClaimService.GetPreferredAsync(user.Id);
        if (preferredResult.Success && preferredResult.Data != null)
        {
            var method = preferredResult.Data.Method;
            await _otpService.GenerateAndSendOtpAsync(user, method);

            var otpToken = _otpService.GenerateToken(request.UsernameOrEmail, request.RememberMe, preferredResult.Data.Method);
            return OperationResult<(string, string)?>.Ok((AccessToken: otpToken, RefreshToken: string.Empty));
        }

        // ---- Kein 2FA, JWT + RefreshToken generieren ----
        var jwtToken = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, request.DeviceName, request.IpAddress, request.RememberMe);

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return OperationResult<(string, string)?>.Ok((jwtToken, refreshToken.Token));
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
        var jwtToken = _jwtTokenService.GenerateToken(user);
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
}