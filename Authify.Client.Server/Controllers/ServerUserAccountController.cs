using System.Security.Claims;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Client.Server.Controllers;

[ApiController]
[Route("api/server-useraccount")]
[Authorize]
public class ServerUserAccountController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;

    public ServerUserAccountController(IUserAccountService userAccountService)
    {
        _userAccountService = userAccountService;
    }

    private string? GetUserId() => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpPost("export")]
    public async Task<IActionResult> RequestExport()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userAccountService.RequestExportAsync(userId);
        if (!result.Success) return BadRequest(result.ErrorMessage);

        return File(result.Data!, "application/zip", "user-data-export.zip");
    }

    [HttpGet("export/status")]
    public async Task<IActionResult> GetExportStatus()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userAccountService.GetExportStatusAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> DeactivateAccount()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userAccountService.DeactivateAccountAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("deactivate/status")]
    public async Task<IActionResult> GetDeactivationStatus()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userAccountService.GetDeactivationStatusAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userAccountService.DeleteAccountAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("delete/status")]
    public async Task<IActionResult> GetDeletionStatus()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userAccountService.GetDeletionStatusAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
