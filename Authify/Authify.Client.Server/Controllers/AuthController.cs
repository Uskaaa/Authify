using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Authify.Client.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthServiceCookie _authServiceCookie;

    public AuthController(IAuthServiceCookie authServiceCookie)
    {
        _authServiceCookie = authServiceCookie;
    }

    [HttpPost(nameof(Login))]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        loginRequest.DeviceName = Request.Headers["Device-Name"].FirstOrDefault() ?? "Unknown";
        loginRequest.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var result = await _authServiceCookie.LoginAsync(loginRequest);

        if (!result.Success)
            return BadRequest(result);

        var loginResponse = new LoginResponseDto
        {
            ResultKind = LoginResultKind.Cookie,
            CookieSet = string.IsNullOrEmpty(result.Data),
            Message = string.IsNullOrEmpty(result.Data) ? "Cookie authentication succeeded." : "OTP required.",
            AccessToken = result.Data // OTP token if 2FA is required, null otherwise
        };

        return Ok(loginResponse);
    }

    [HttpPost(nameof(VerifyOtp))]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationRequest request)
    {
        var result = await _authServiceCookie.VerifyOtpAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost(nameof(ResendOtp))]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        var result = await _authServiceCookie.ResendOtpAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost(nameof(Logout))]
    public async Task<IActionResult> Logout()
    {
        var result = await _authServiceCookie.LogoutAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
