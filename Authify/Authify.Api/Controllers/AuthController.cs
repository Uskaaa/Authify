using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[Controller]")]
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

        if (result.Success) return Ok(new { myessage = result.Data });
        
        return BadRequest("Wrong username or password.");
    }

    [HttpPost(nameof(VerifyOtp))]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationRequest otpVerificationRequest)
    {
        var result = await _authService.VerifyOtpAsync(otpVerificationRequest);

        if (result.Success) return Ok();
        
        return BadRequest("Wrong otp code.");
    }
}