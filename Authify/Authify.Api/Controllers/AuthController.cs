using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Authify.Api.Controllers;

[ApiController]
[EnableRateLimiting("AuthPolicy")]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthServiceJwt _authServiceJwt;

    public AuthController(IAuthServiceJwt authServiceJwt)
    {
        _authServiceJwt = authServiceJwt;
    }

    [HttpPost(nameof(Login))]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        // DeviceName/IP optional aus Request oder Header
        loginRequest.DeviceName = Request.Headers["Device-Name"].FirstOrDefault() ?? "Unknown";
        loginRequest.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var result = await _authServiceJwt.LoginAsync(loginRequest);

        if (!result.Success)
            return BadRequest(result);

        var (accessToken, refreshToken) = result.Data!.Value;

        var loginResponse = new LoginResponseDto()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ResultKind = LoginResultKind.Jwt
        };

        return Ok(OperationResult<LoginResponseDto>.Ok(loginResponse));;
    }

    [HttpPost(nameof(VerifyOtp))]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationRequest request)
    {
        var result = await _authServiceJwt.VerifyOtpAsync(request);

        if (!result.Success)
            return BadRequest(result);

        var (accessToken, refreshToken) = result.Data!.Value;
        
        var otpResponse = new OtpResponseDto()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
        
        return Ok(OperationResult<OtpResponseDto>.Ok(otpResponse));
    }

    [HttpPost(nameof(ResendOtp))]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        var result = await _authServiceJwt.ResendOtpAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost(nameof(Logout))]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        await _authServiceJwt.LogoutAsync(refreshToken);
        return Ok();
    }
}