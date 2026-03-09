using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Client.Server.Controllers;

/// <summary>
/// HTTP endpoints for cookie-based authentication.
/// These must run in a real HTTP request context so that ASP.NET Core Identity
/// can write the HttpOnly authentication cookie to the response.
/// </summary>
[ApiController]
[Route("api/server-auth")]
public class CookieAuthController : ControllerBase
{
    private readonly IAuthServiceCookie _cookieAuthService;

    public CookieAuthController(IAuthServiceCookie cookieAuthService)
    {
        _cookieAuthService = cookieAuthService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        request.DeviceName ??= Request.Headers["Device-Name"].FirstOrDefault() ?? "Unknown";
        request.IpAddress ??= HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var result = await _cookieAuthService.LoginAsync(request);
        if (!result.Success)
            return BadRequest(OperationResult<LoginResponseDto>.Fail(result.ErrorMessage ?? "Login failed."));

        if (string.IsNullOrEmpty(result.Data))
        {
            // No OTP required – SignInManager already set the auth cookie on this HTTP response
            return Ok(OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
            {
                ResultKind = LoginResultKind.Cookie,
                CookieSet = true,
                Message = "Cookie authentication succeeded."
            }));
        }

        // OTP required – Data contains the temporary OTP token
        return Ok(OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
        {
            ResultKind = LoginResultKind.Cookie,
            Message = "OTP required.",
            AccessToken = result.Data
        }));
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationRequest request)
    {
        var result = await _cookieAuthService.VerifyOtpAsync(request);
        if (!result.Success)
            return BadRequest(OperationResult<OtpResponseDto>.Fail(result.ErrorMessage ?? "OTP verification failed."));

        // SignInManager already set the auth cookie on this HTTP response
        return Ok(OperationResult<OtpResponseDto>.Ok(new OtpResponseDto()));
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        var result = await _cookieAuthService.ResendOtpAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // SignInManager.SignOutAsync clears the auth cookie via this HTTP response
        var result = await _cookieAuthService.LogoutAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
