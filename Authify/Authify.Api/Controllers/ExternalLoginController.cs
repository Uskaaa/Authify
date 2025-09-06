using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalLoginController<TUser> : ControllerBase
    where TUser : IdentityUser
{
    private readonly IExternalLoginManagementService<TUser> _externalLoginService;
    private readonly UserManager<TUser> _userManager;

    public ExternalLoginController(IExternalLoginManagementService<TUser> externalLoginService, UserManager<TUser> userManager)
    {
        _externalLoginService = externalLoginService;
        _userManager = userManager;
    }

    [HttpGet("connected")]
    public async Task<IActionResult> GetConnectedProviders()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var logins = await _externalLoginService.GetConnectedProvidersAsync(user);
        return Ok(logins);
    }

    [HttpPost("disconnect/{provider}")]
    public async Task<IActionResult> DisconnectProvider(string provider)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var result = await _externalLoginService.DisconnectProviderAsync(user, provider);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok();
    }

    [HttpPost("connect")]
    public async Task<IActionResult> ConnectProvider([FromBody] UserLoginInfo loginInfo)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var result = await _externalLoginService.ConnectProviderAsync(user, loginInfo);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok();
    }
}