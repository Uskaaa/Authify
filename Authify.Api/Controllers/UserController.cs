using System.Security.Claims;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[Controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpPost(nameof(Register))]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        var result = await _userService.RegisterAsync(registerRequest);

        if (result.Success) return Ok(result);
        
        return BadRequest(result);
    }

    [HttpPost(nameof(ConfirmEmail))]
    public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmationRequest emailConfirmationRequest)
    {
        var result = await _userService.ConfirmEmailAsync(emailConfirmationRequest);
        
        if (result.Success) return Ok(result);
        
        return BadRequest("Something went wrong.");
    }
    
    [HttpPost(nameof(ForgotPassword))]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
    {
        var result = await _userService.ForgotPasswordAsync(forgotPasswordRequest);
        
        if (result.Success) return Ok(result);
        
        return BadRequest("Something went wrong.");
    }
    
    [HttpPost(nameof(ResetPassword))]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
    {
        var result = await _userService.ResetPasswordAsync(resetPasswordRequest);
        
        if (result.Success) return Ok(result);
        
        return BadRequest("Something went wrong.");
    }
    
    [Authorize]
    [HttpPost(nameof(ChangePassword))]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest changePasswordRequest)
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var result = await _userService.ChangePasswordAsync(userId, changePasswordRequest);
        
        if (result.Success) return Ok(result);
        
        return BadRequest(result);
    }
}