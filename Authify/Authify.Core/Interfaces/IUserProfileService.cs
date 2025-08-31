using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IUserProfileService
{
    Task<OperationResult> UpdatePersonalInformationAsync(string userId, PersonalInformationUpdateRequest request);
    Task<OperationResult> UpdateProfileImageAsync(string userId, ProfileImageUpdateRequest request);
    Task<OperationResult<UserProfileDto>> GetProfileAsync(string userId);
}