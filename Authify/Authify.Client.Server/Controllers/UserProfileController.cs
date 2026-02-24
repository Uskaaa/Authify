using System.Security.Claims;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Client.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public UserProfileController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    private string GetUserId()
    {
        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User not authenticated");
        return userId;
    }

    [HttpPost("update-personal-information")]
    public async Task<IActionResult> UpdatePersonalInformation([FromBody] PersonalInformationUpdateRequest request)
    {
        var result = await _userProfileService.UpdatePersonalInformationAsync(GetUserId(), request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update-profile-image")]
    public async Task<IActionResult> UpdateProfileImage([FromBody] ProfileImageUpdateRequest request)
    {
        var result = await _userProfileService.UpdateProfileImageAsync(GetUserId(), request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _userProfileService.GetProfileAsync(GetUserId());
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("send-phone-verification")]
    public async Task<IActionResult> SendPhoneVerification()
    {
        var result = await _userProfileService.SendPhoneVerificationCodeAsync(GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhone([FromBody] PhoneVerificationRequest request)
    {
        var result = await _userProfileService.VerifyPhoneNumberAsync(GetUserId(), request.Code);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
