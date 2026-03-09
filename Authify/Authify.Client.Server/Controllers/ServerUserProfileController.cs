using System.Security.Claims;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Client.Server.Controllers;

[ApiController]
[Route("api/server-userprofile")]
[Authorize]
public class ServerUserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public ServerUserProfileController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    private string? GetUserId() => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userProfileService.GetProfileAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update-personal")]
    public async Task<IActionResult> UpdatePersonalInformation([FromBody] PersonalInformationUpdateRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userProfileService.UpdatePersonalInformationAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update-image")]
    public async Task<IActionResult> UpdateProfileImage([FromBody] ProfileImageUpdateRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userProfileService.UpdateProfileImageAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("send-phone-verification")]
    public async Task<IActionResult> SendPhoneVerificationCode()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userProfileService.SendPhoneVerificationCodeAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhoneNumber([FromBody] PhoneVerificationRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _userProfileService.VerifyPhoneNumberAsync(userId, request.Code);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
