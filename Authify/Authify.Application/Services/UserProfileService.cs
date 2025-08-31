using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class UserProfileService<TUser> : IUserProfileService
    where TUser : IdentityUser
{
    private readonly UserManager<TUser> _userManager;
    private readonly IAuthifyDbContext _context;

    public UserProfileService(UserManager<TUser> userManager, IAuthifyDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<OperationResult> UpdatePersonalInformationAsync(string userId,
        PersonalInformationUpdateRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            await _context.UserProfiles.AddAsync(profile);
        }

        profile.FullName = request.FullName;
        profile.PhoneNumber = request.PhoneNumber;
        profile.JobTitle = request.JobTitle;
        profile.Company = request.Company;
        profile.Bio = request.Bio;

        // E-Mail ändern
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            user.Email = request.Email;
            user.EmailConfirmed = false;
            // ConfirmEmailAsync kann später für Bestätigung genutzt werden
        }

        if (!string.IsNullOrEmpty(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;

        await _context.SaveChangesAsync();

        return OperationResult.Ok();
    }

    public async Task<OperationResult> UpdateProfileImageAsync(string userId, ProfileImageUpdateRequest request)
    {
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            await _context.UserProfiles.AddAsync(profile);
        }

        profile.ProfileImage = request.Image; // null = löschen

        await _context.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult<UserProfileDto>> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult<UserProfileDto>.Fail("User not found.");

        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        var dto = new UserProfileDto
        {
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = profile?.FullName,
            JobTitle = profile?.JobTitle,
            Company = profile?.Company,
            Bio = profile?.Bio,
            ProfileImage = profile?.ProfileImage
        };

        return OperationResult<UserProfileDto>.Ok(dto);
    }
}