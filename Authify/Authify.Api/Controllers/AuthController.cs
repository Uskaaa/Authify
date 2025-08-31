using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost(nameof(Login))]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var result = await _authService.LoginAsync(loginRequest);

        if (result.Success)
            return Ok(new { token = result.Data });

        return BadRequest(new { error = result.ErrorMessage ?? "Wrong username or password." });
    }

    [HttpPost(nameof(VerifyOtp))]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationRequest request)
    {
        var result = await _authService.VerifyOtpAsync(request);

        if (result.Success)
            return Ok(new { token = result.Data });

        return BadRequest(new { error = result.ErrorMessage ?? "Invalid OTP code." });
    }

    [HttpPost(nameof(ResendOtp))]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        var result = await _authService.ResendOtpAsync(request);

        if (result.Success)
            return Ok();

        return BadRequest(new { error = result.ErrorMessage ?? "Could not resend OTP." });
    }

    [HttpPost(nameof(Logout))]
    public async Task<IActionResult> Logout()
    {
        var result = await _authService.LogoutAsync();

        if (result.Success)
            return Ok();

        return BadRequest(new { error = result.ErrorMessage ?? "Logout failed." });
    }
}