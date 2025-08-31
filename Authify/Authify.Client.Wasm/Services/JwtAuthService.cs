using Authify.Application.Data;
using Authify.Application.Services;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Models.Enums;
using Authify.Core.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Client.Wasm.Services;

public class JwtAuthService<TUser> : IAuthService
    where TUser : IdentityUser
{
    private readonly IOtpService _otpService;
    private readonly SignInManager<TUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly UserManager<TUser> _userManager;
    private readonly TwoFactorClaimService<TUser> _twoFactorClaimService;
    private readonly IUserAccountService _userAccountService;
    private readonly IAuthifyDbContext _context;

    public JwtAuthService(IOtpService otpService, SignInManager<TUser> signInManager, IJwtTokenService jwtTokenService,
        TwoFactorClaimService<TUser> twoFactorClaimService,
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

    public async Task<OperationResult<string>> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.UsernameOrEmail);
        if (user == null)
            return OperationResult<string>.Fail("Invalid login attempt.");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return OperationResult<string>.Fail("Please confirm your E-Mail first.");

        var deactivation = await _userAccountService.GetDeactivationStatusAsync(user.Id);
        if (deactivation.Data != null && deactivation.Data.IsDeactivated)
            return OperationResult<string>.Fail("Your account is deactivated. Please contact support!");
        
        var resultCheckSignIn =
            await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        if (!resultCheckSignIn.Succeeded)
            return OperationResult<string>.Fail("Ungültige Anmeldedaten!");

        // 2FA prüfen
        var preferredResult = await _twoFactorClaimService.GetPreferredAsync(user.Id);
        if (preferredResult.Success && preferredResult.Data != null)
        {
            // Bevorzugte Methode auswählen
            var method = preferredResult.Data.Method;
            await _otpService.GenerateAndSendOtpAsync(request.UsernameOrEmail, method);
        }
        else
        {
            var jwtToken = _jwtTokenService.GenerateToken(user);
            return OperationResult<string>.Ok(jwtToken);
        }

        var token = _otpService.GenerateToken(request.UsernameOrEmail, request.RememberMe);

        return OperationResult<string>.Ok(token);
    }

    public async Task<OperationResult<string>> VerifyOtpAsync(OtpVerificationRequest request)
    {
        var (email, rememberMe) = _otpService.ValidateToken(request.Token);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return OperationResult<string>.Fail("Benutzer nicht gefunden.");

        var isValid = await _otpService.ValidateOtpAsync(email, request.OtpCode);
        if (!isValid)
            return OperationResult<string>.Fail("Invalid OTP code.");

        var jwtToken = _jwtTokenService.GenerateToken(user);

        return OperationResult<string>.Ok(jwtToken);
    }

    public async Task<OperationResult> ResendOtpAsync(ResendOtpRequest request)
    {
        // Token validieren (optional, abhängig von deiner Logik)
        var (email, rememberMe) = _otpService.ValidateToken(request.Token);

        if (!string.Equals(email, request.UsernameOrEmail, StringComparison.OrdinalIgnoreCase))
            return OperationResult.Fail("Token does not match user.");

        // OTP senden mit gewünschter Methode
        await _otpService.GenerateAndSendOtpAsync(request.UsernameOrEmail, request.TwoFactorMethod);

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