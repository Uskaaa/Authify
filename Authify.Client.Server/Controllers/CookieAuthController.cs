using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Client.Server.Controllers;

/// <summary>
/// HTTP endpoints for cookie-based authentication using a secure redirect pattern.
///
/// Problem: ASP.NET Core Identity's SignInManager.SignInAsync writes the HttpOnly
/// auth cookie to the HTTP response. In Blazor Server Interactive mode the browser
/// is connected via WebSocket (HTTP 101 Upgrade) – the response headers have already
/// been sent, so cookies cannot be appended.
///
/// Solution: Split the flow in two steps:
///   1. POST /api/server-auth/login   – validates credentials, returns a short-lived
///      DataProtection token as a redirect URL.
///   2. GET  /api/server-auth/complete-login – the browser follows the redirect as a
///      real HTTP GET; the controller calls SignInAsync here so Identity can write
///      the Set-Cookie header to this fresh HTTP response.
/// </summary>
[ApiController]
[Route("api/server-auth")]
public class CookieAuthController : ControllerBase
{
    private const string PurposePendingLogin = "AuthifyPendingLogin";
    private const string PurposePendingOtp = "AuthifyPendingOtp";

    private readonly IAuthServiceCookie _cookieAuthService;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public CookieAuthController(
        IAuthServiceCookie cookieAuthService,
        IDataProtectionProvider dataProtectionProvider)
    {
        _cookieAuthService = cookieAuthService;
        _dataProtectionProvider = dataProtectionProvider;
    }

    // ------------------------------------------------------------------
    // Step 1: validate credentials → return redirect URL with signed token
    // ------------------------------------------------------------------

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        request.DeviceName ??= Request.Headers["Device-Name"].FirstOrDefault() ?? "Unknown";
        request.IpAddress ??= HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var result = await _cookieAuthService.ValidateCredentialsAsync(request);
        if (!result.Success)
            return BadRequest(OperationResult<LoginResponseDto>.Fail(result.ErrorMessage ?? "Login failed."));

        var pending = result.Data!;

        if (pending.RequiresOtp)
        {
            return Ok(OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
            {
                ResultKind = LoginResultKind.Cookie,
                Message = "OTP required.",
                AccessToken = pending.OtpToken
            }));
        }

        // Create a short-lived, tamper-proof pending-login token
        var protector = _dataProtectionProvider
            .CreateProtector(PurposePendingLogin)
            .ToTimeLimitedDataProtector();

        var raw = $"{pending.UserId}|{pending.IsPersistent}";
        var token = protector.Protect(raw, lifetime: TimeSpan.FromMinutes(5));

        var redirectUrl = $"/api/server-auth/complete-login?token={Uri.EscapeDataString(token)}";

        return Ok(OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
        {
            ResultKind = LoginResultKind.Cookie,
            CookieSet = false,
            RedirectUrl = redirectUrl,
            Message = "Redirect to complete cookie authentication."
        }));
    }

    // ------------------------------------------------------------------
    // Step 2: the browser follows the redirect → SignInAsync writes cookie
    // ------------------------------------------------------------------

    [HttpGet("complete-login")]
    public async Task<IActionResult> CompleteLogin([FromQuery] string token, [FromQuery] string? returnUrl = null)
    {
        try
        {
            var protector = _dataProtectionProvider
                .CreateProtector(PurposePendingLogin)
                .ToTimeLimitedDataProtector();

            var raw = protector.Unprotect(token);
            var parts = raw.Split('|');
            if (parts.Length < 2)
                return Redirect("/login?error=invalid_token");

            var userId = parts[0];
            var isPersistent = bool.Parse(parts[1]);

            var signInResult = await _cookieAuthService.CompleteSignInAsync(userId, isPersistent);
            if (!signInResult.Success)
                return Redirect("/login?error=signin_failed");

            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
            return Redirect(safeReturnUrl!);
        }
        catch
        {
            return Redirect("/login?error=invalid_token");
        }
    }

    // ------------------------------------------------------------------
    // OTP: validate OTP → return redirect URL
    // ------------------------------------------------------------------

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationRequest request)
    {
        var result = await _cookieAuthService.ValidateOtpAsync(request);
        if (!result.Success)
            return BadRequest(OperationResult<OtpResponseDto>.Fail(result.ErrorMessage ?? "OTP verification failed."));

        var pending = result.Data!;

        var protector = _dataProtectionProvider
            .CreateProtector(PurposePendingOtp)
            .ToTimeLimitedDataProtector();

        var raw = $"{pending.UserId}|{pending.IsPersistent}";
        var token = protector.Protect(raw, lifetime: TimeSpan.FromMinutes(5));

        var redirectUrl = $"/api/server-auth/complete-otp?token={Uri.EscapeDataString(token)}";

        return Ok(OperationResult<OtpResponseDto>.Ok(new OtpResponseDto
        {
            RedirectUrl = redirectUrl
        }));
    }

    [HttpGet("complete-otp")]
    public async Task<IActionResult> CompleteOtp([FromQuery] string token, [FromQuery] string? returnUrl = null)
    {
        try
        {
            var protector = _dataProtectionProvider
                .CreateProtector(PurposePendingOtp)
                .ToTimeLimitedDataProtector();

            var raw = protector.Unprotect(token);
            var parts = raw.Split('|');
            if (parts.Length < 2)
                return Redirect("/otp?error=invalid_token");

            var userId = parts[0];
            var isPersistent = bool.Parse(parts[1]);

            var signInResult = await _cookieAuthService.CompleteSignInAsync(userId, isPersistent);
            if (!signInResult.Success)
                return Redirect("/otp?error=signin_failed");

            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
            return Redirect(safeReturnUrl!);
        }
        catch
        {
            return Redirect("/otp?error=invalid_token");
        }
    }

    // ------------------------------------------------------------------
    // Resend OTP & Logout (no cookie manipulation needed for resend)
    // ------------------------------------------------------------------

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        var result = await _cookieAuthService.ResendOtpAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Logout must also be called from a real HTTP request so that Identity can
    /// clear the HttpOnly cookie via the HTTP response.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await _cookieAuthService.LogoutAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
