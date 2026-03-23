using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Extensions;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class UserProfileService<TUser> : IUserProfileService
    where TUser : ApplicationUser
{
    private readonly UserManager<TUser> _userManager;
    private readonly IAuthifyDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly InfrastructureOptions _infrastructureOptions;

    public UserProfileService(UserManager<TUser> userManager, IAuthifyDbContext context, IEmailSender emailSender, ISmsSender smsSender, InfrastructureOptions infrastructureOptions)
    {
        _userManager = userManager;
        _context = context;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _infrastructureOptions = infrastructureOptions;
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
        profile.JobTitle = request.JobTitle;
        profile.Company = request.Company;
        profile.Bio = request.Bio;

        // Phone number change: reset confirmation
        if (request.PhoneNumber != user.PhoneNumber)
        {
            user.PhoneNumber = request.PhoneNumber;
            user.PhoneNumberConfirmed = false;
        }

        profile.PhoneNumber = user.PhoneNumber;

        // E-Mail ändern
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            user.Email = request.Email;
            user.EmailConfirmed = false;
            
            var confirmationLink =
                $"https://{_infrastructureOptions.Domain}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
            await _emailSender.SendEmailAsync(user.Email!, "Confirm your email",
                $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.");
        }

        await _userManager.UpdateAsync(user);
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
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            FullName = user.FullName,
            JobTitle = profile?.JobTitle,
            Company = profile?.Company,
            Bio = profile?.Bio,
            ProfileImage = profile?.ProfileImage
        };

        return OperationResult<UserProfileDto>.Ok(dto);
    }

    public async Task<OperationResult> SendPhoneVerificationCodeAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        if (string.IsNullOrEmpty(user.PhoneNumber))
            return OperationResult.Fail("No phone number set.");

        var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
        await _smsSender.SendSmsAsync(user.PhoneNumber, token);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> VerifyPhoneNumberAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        if (string.IsNullOrEmpty(user.PhoneNumber))
            return OperationResult.Fail("No phone number set.");

        var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, code);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return OperationResult.Fail(errors);
        }

        return OperationResult.Ok();
    }
}
