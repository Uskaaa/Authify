using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Server.Models;
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

        if (result.Success) return Ok(); 
        
        return BadRequest("Something went wrong.");
    }

    [HttpGet(nameof(ConfirmEmail))]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        EmailConfirmationRequest emailConfirmationRequest = new()
        {
            UserId = userId,
            Token = token
        };
        var result = await _userService.ConfirmEmailAsync(emailConfirmationRequest);
        
        if (result.Success) return Ok();
        
        return BadRequest("Something went wrong.");
    }
    
    [HttpPost(nameof(ForgotPassword))]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
    {
        var result = await _userService.ForgotPasswordAsync(forgotPasswordRequest);
        
        if (result.Success) return Ok();
        
        return BadRequest("Something went wrong.");
    }

    [HttpPost(nameof(ResetPassword))]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
    {
        var result = await _userService.ResetPasswordAsync(resetPasswordRequest);
        
        if (result.Success) return Ok();
        
        return BadRequest("Something went wrong.");
    }
}