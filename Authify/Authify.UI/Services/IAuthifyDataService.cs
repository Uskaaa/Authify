using Authify.Core.Common;
using Authify.Core.Models;


namespace Authify.UI.Services;

/// <summary>
/// Platform-independent service interface for Authify data operations.
/// This interface uses only platform-agnostic types (no ASP.NET Core dependencies).
/// </summary>
public interface IAuthifyDataService
{
    //AuthService
    [System.Obsolete("Use LoginAsync(LoginRequest) returning LoginResponseDto")]
    Task<OperationResult<(string AccessToken, string RefreshToken)?>> JwtLoginAsync(LoginRequest request);

    [System.Obsolete("Use LoginAsync(LoginRequest) returning LoginResponseDto")]
    Task<OperationResult<string>> CookieLoginAsync(LoginRequest loginRequest);

    Task<OperationResult<LoginResponseDto>> LoginAsync(LoginRequest request);

    Task<OperationResult<OtpResponseDto>> JwtVerifyOtpAsync(OtpVerificationRequest request);
    Task<OperationResult<string>> CookieVerifyOtpAsync(OtpVerificationRequest request);
    Task<OperationResult> ResendOtpAsync(ResendOtpRequest request);
    Task<OperationResult> JwtLogoutAsync();
    Task<OperationResult> CookieLogoutAsync();
    
    //ExternalAuth
    Task<string?> GetAccessTokenAsync();
    Task<OperationResult> StoreTokensFromExternalAuth(string accessToken, string refreshToken);

    //UserService
    Task<OperationResult> RegisterAsync(RegisterRequest registerRequest);
    Task<OperationResult> ConfirmEmailAsync(EmailConfirmationRequest emailConfirmationRequest);
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
    Task<OperationResult> SendPhoneVerificationCodeAsync();
    Task<OperationResult> VerifyPhoneNumberAsync(string code);

    //TwoFactorClaimService
    Task<OperationResult> AddOrUpdateAsync(TwoFactorRequest request);
    Task<OperationResult> RemoveAsync(TwoFactorRequest request);
    Task<OperationResult<List<UserTwoFactor>>> GetAllAsync();
    Task<OperationResult<UserTwoFactor>> GetPreferredAsync();

    //ExternalLoginManagementService - Platform-independent
    Task<OperationResult<List<ExternalLoginDto>>> GetConnectedProvidersAsync();
    Task<OperationResult> DisconnectProviderAsync(string provider);
    Task<OperationResult> ConnectProviderAsync(ConnectExternalLoginRequest request);
}
