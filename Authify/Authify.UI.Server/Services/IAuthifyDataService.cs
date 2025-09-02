using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Authify.UI.Server.Services;

public interface IAuthifyDataService
{
    //External Auth
    Task<IActionResult> ExternalLoginAsync(string provider, string? returnUrl = "/");
    
    //AuthService
    Task<OperationResult<(string AccessToken, string RefreshToken)?>> JwtLoginAsync(LoginRequest request);
    Task<OperationResult<string>> CookieLoginAsync(LoginRequest loginRequest);
    Task<OperationResult<(string AccessToken, string RefreshToken)?>> JwtVerifyOtpAsync(OtpVerificationRequest request);
    Task<OperationResult<string>> CookieVerifyOtpAsync(OtpVerificationRequest request);
    Task<OperationResult> ResendOtpAsync(ResendOtpRequest request);
    Task JwtLogoutAsync(string refreshToken);
    Task<OperationResult> CookieLogoutAsync();
    
    //UserService
    Task<OperationResult> RegisterAsync(RegisterRequest registerRequest);
    Task<OperationResult> ConfirmEmailAsync(EmailConfirmationRequest  emailConfirmationRequest);
    Task<OperationResult> ForgotPasswordAsync(ForgotPasswordRequest forgotPasswordRequest);
    Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest);
    Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request);
    
    //UserAccountService
    Task<OperationResult<byte[]>> RequestExportAsync();
    Task<OperationResult<UserExportRequest>> GetExportStatusAsync();
    Task<OperationResult> DeactivateAccountAsync();
    Task<OperationResult<UserDeactivationRequest>> GetDeactivationStatusAsync();
    Task<OperationResult> DeleteAccountAsync();
    Task<OperationResult<UserDeletionRequest>> GetDeletionStatusAsync();
    
    //UserProfileService
    Task<OperationResult> UpdatePersonalInformationAsync(PersonalInformationUpdateRequest request);
    Task<OperationResult> UpdateProfileImageAsync(ProfileImageUpdateRequest request);
    Task<OperationResult<UserProfileDto>> GetProfileAsync();
    
    //TwoFactorClaimService
    Task<OperationResult> AddOrUpdateAsync(TwoFactorRequest request);
    Task<OperationResult> RemoveAsync(TwoFactorRequest request);
    Task<OperationResult<List<UserTwoFactor>>> GetAllAsync();
    Task<OperationResult<UserTwoFactor>> GetPreferredAsync();
}