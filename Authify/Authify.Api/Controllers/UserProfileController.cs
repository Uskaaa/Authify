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

        return result.Success ? Ok(result) : BadRequest(result.ErrorMessage);
    }

    // ---- Profilbild aktualisieren ----
    [HttpPost("update-profile-image")]
    public async Task<IActionResult> UpdateProfileImage([FromBody] ProfileImageUpdateRequest request)
    {
        var userId = GetUserId();
        var result = await _userProfileService.UpdateProfileImageAsync(userId, request);

        return result.Success ? Ok(result) : BadRequest(result.ErrorMessage);
    }

    // ---- Profilinformationen abrufen ----
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var result = await _userProfileService.GetProfileAsync(userId);

        return result.Success ? Ok(result.Data) : NotFound(result.ErrorMessage);
    }
}