using System.Security.Claims;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

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
        // ClaimTypes.NameIdentifier enthält die UserId bei Identity
        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User not authenticated");
        return userId;
    }

    // ---- Personal Information aktualisieren ----
    [HttpPost("update-personal-information")]
    public async Task<IActionResult> UpdatePersonalInformation([FromBody] PersonalInformationUpdateRequest request)
    {
        var userId = GetUserId();
        var result = await _userProfileService.UpdatePersonalInformationAsync(userId, request);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ---- Profilbild aktualisieren ----
    [HttpPost("update-profile-image")]
    public async Task<IActionResult> UpdateProfileImage([FromBody] ProfileImageUpdateRequest request)
    {
        var userId = GetUserId();
        var result = await _userProfileService.UpdateProfileImageAsync(userId, request);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ---- Profilinformationen abrufen ----
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var result = await _userProfileService.GetProfileAsync(userId);

        return result.Success ? Ok(result) : NotFound(result);
    }

    // ---- Telefonnummer Bestätigungscode senden ----
    [HttpPost("send-phone-verification")]
    public async Task<IActionResult> SendPhoneVerification()
    {
        var userId = GetUserId();
        var result = await _userProfileService.SendPhoneVerificationCodeAsync(userId);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ---- Telefonnummer verifizieren ----
    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhone([FromBody] PhoneVerificationRequest request)
    {
        var userId = GetUserId();
        var result = await _userProfileService.VerifyPhoneNumberAsync(userId, request.Code);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}
