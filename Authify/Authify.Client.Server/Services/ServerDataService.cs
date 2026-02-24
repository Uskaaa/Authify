using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.UI.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Authify.Client.Server.Services;

public class ServerDataService<TUser> : IAuthifyDataService where TUser : ApplicationUser, new()
{
    private readonly IAuthServiceCookie _authServiceCookie;
    private readonly IUserService _userService;
    private readonly IUserAccountService _userAccountService;
    private readonly IUserProfileService _userProfileService;
    private readonly ITwoFactorClaimService _twoFactorClaimService;
    private readonly IExternalLoginManagementService _externalLoginManagementService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerDataService(
        IAuthServiceCookie authServiceCookie,
        IUserService userService,
        IUserAccountService userAccountService,
        IUserProfileService userProfileService,
        ITwoFactorClaimService twoFactorClaimService,
        IExternalLoginManagementService externalLoginManagementService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authServiceCookie = authServiceCookie;
        _userService = userService;
        _userAccountService = userAccountService;
        _userProfileService = userProfileService;
        _twoFactorClaimService = twoFactorClaimService;
        _externalLoginManagementService = externalLoginManagementService;
        _httpContextAccessor = httpContextAccessor;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // ---- Auth ----

    [System.Obsolete("Use LoginAsync(LoginRequest) returning LoginResponseDto")]
    public async Task<OperationResult<(string AccessToken, string RefreshToken)?>> JwtLoginAsync(LoginRequest request)
    {
        var loginResult = await LoginAsync(request);
        if (!loginResult.Success || loginResult.Data?.ResultKind != LoginResultKind.Jwt)
            return OperationResult<(string AccessToken, string RefreshToken)?>.Fail(loginResult.ErrorMessage ?? "JWT login is not supported in server-side cookie mode.");

        return OperationResult<(string AccessToken, string RefreshToken)?>.Ok((loginResult.Data.AccessToken!, loginResult.Data.RefreshToken!));
    }

    [System.Obsolete("Use LoginAsync(LoginRequest) returning LoginResponseDto")]
    public async Task<OperationResult<string>> CookieLoginAsync(LoginRequest loginRequest)
    {
        var loginResult = await LoginAsync(loginRequest);
        if (!loginResult.Success)
            return OperationResult<string>.Fail(loginResult.ErrorMessage ?? "Cookie login failed.");

        return OperationResult<string>.Ok(loginResult.Data?.CookieName ?? string.Empty);
    }

    public async Task<OperationResult<LoginResponseDto>> LoginAsync(LoginRequest request)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http != null)
        {
            request.DeviceName ??= http.Request.Headers["Device-Name"].FirstOrDefault() ?? "Unknown";
            request.IpAddress ??= http.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        var cookieResult = await _authServiceCookie.LoginAsync(request);
        if (!cookieResult.Success)
            return OperationResult<LoginResponseDto>.Fail(cookieResult.ErrorMessage ?? "Login failed.");

        if (string.IsNullOrEmpty(cookieResult.Data))
        {
            return OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
            {
                ResultKind = LoginResultKind.Cookie,
                CookieSet = true,
                Message = "Cookie authentication succeeded."
            });
        }

        // OTP required – Data contains the temporary OTP token
        return OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
        {
            ResultKind = LoginResultKind.Cookie,
            Message = "OTP required.",
            AccessToken = cookieResult.Data
        });
    }

    public async Task<OperationResult<OtpResponseDto>> JwtVerifyOtpAsync(OtpVerificationRequest request)
    {
        var result = await _authServiceCookie.VerifyOtpAsync(request);
        if (!result.Success)
            return OperationResult<OtpResponseDto>.Fail(result.ErrorMessage ?? "OTP verification failed.");

        // Cookie-based: no JWT tokens – return empty DTO to indicate success
        return OperationResult<OtpResponseDto>.Ok(new OtpResponseDto());
    }

    public async Task<OperationResult<string>> CookieVerifyOtpAsync(OtpVerificationRequest request)
    {
        var result = await _authServiceCookie.VerifyOtpAsync(request);
        return result.Success
            ? OperationResult<string>.Ok("Cookie set successfully.")
            : OperationResult<string>.Fail(result.ErrorMessage ?? "OTP verification failed.");
    }

    public Task<OperationResult> ResendOtpAsync(ResendOtpRequest request)
        => _authServiceCookie.ResendOtpAsync(request);

    /// <summary>Cookie-based logout. In server mode there is no JWT to invalidate.</summary>
    public Task<OperationResult> JwtLogoutAsync() => _authServiceCookie.LogoutAsync();

    public Task<OperationResult> CookieLogoutAsync() => _authServiceCookie.LogoutAsync();

    // ---- External Auth token helpers (no-ops in cookie mode) ----

    /// <summary>Returns null – server-side cookie auth does not issue JWT access tokens.</summary>
    public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(null);

    /// <summary>No-op – tokens are not stored server-side; the cookie is set automatically by SignInAsync.</summary>
    public Task<OperationResult> StoreTokensFromExternalAuth(string accessToken, string refreshToken)
        => Task.FromResult(OperationResult.Ok());

    // ---- UserService ----

    public Task<OperationResult> RegisterAsync(RegisterRequest registerRequest) => _userService.RegisterAsync(registerRequest);

    public Task<OperationResult> ConfirmEmailAsync(EmailConfirmationRequest emailConfirmationRequest) => _userService.ConfirmEmailAsync(emailConfirmationRequest);

    public Task<OperationResult> ForgotPasswordAsync(ForgotPasswordRequest forgotPasswordRequest) => _userService.ForgotPasswordAsync(forgotPasswordRequest);

    public Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest) => _userService.ResetPasswordAsync(resetPasswordRequest);

    public Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _userService.ChangePasswordAsync(userId, request);
    }

    // ---- UserAccountService ----

    public Task<OperationResult<byte[]>> RequestExportAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult<byte[]>.Fail("User not authenticated."));

        return _userAccountService.RequestExportAsync(userId);
    }

    public Task<OperationResult<UserExportRequest>> GetExportStatusAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult<UserExportRequest>.Fail("User not authenticated."));

        return _userAccountService.GetExportStatusAsync(userId);
    }

    public Task<OperationResult> DeactivateAccountAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _userAccountService.DeactivateAccountAsync(userId);
    }

    public Task<OperationResult<UserDeactivationRequest>> GetDeactivationStatusAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult<UserDeactivationRequest>.Fail("User not authenticated."));

        return _userAccountService.GetDeactivationStatusAsync(userId);
    }

    public Task<OperationResult> DeleteAccountAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _userAccountService.DeleteAccountAsync(userId);
    }

    public Task<OperationResult<UserDeletionRequest>> GetDeletionStatusAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult<UserDeletionRequest>.Fail("User not authenticated."));

        return _userAccountService.GetDeletionStatusAsync(userId);
    }

    // ---- UserProfileService ----

    public Task<OperationResult> UpdatePersonalInformationAsync(PersonalInformationUpdateRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _userProfileService.UpdatePersonalInformationAsync(userId, request);
    }

    public Task<OperationResult> UpdateProfileImageAsync(ProfileImageUpdateRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _userProfileService.UpdateProfileImageAsync(userId, request);
    }

    public Task<OperationResult<UserProfileDto>> GetProfileAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult<UserProfileDto>.Fail("User not authenticated."));

        return _userProfileService.GetProfileAsync(userId);
    }

    public Task<OperationResult> SendPhoneVerificationCodeAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _userProfileService.SendPhoneVerificationCodeAsync(userId);
    }

    public Task<OperationResult> VerifyPhoneNumberAsync(string code)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _userProfileService.VerifyPhoneNumberAsync(userId, code);
    }

    // ---- TwoFactorClaimService ----

    public Task<OperationResult> AddOrUpdateAsync(TwoFactorRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _twoFactorClaimService.AddOrUpdateAsync(userId, request);
    }

    public Task<OperationResult> RemoveAsync(TwoFactorRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _twoFactorClaimService.RemoveAsync(userId, request);
    }

    public Task<OperationResult<List<UserTwoFactor>>> GetAllAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult<List<UserTwoFactor>>.Fail("User not authenticated."));

        return _twoFactorClaimService.GetAllAsync(userId);
    }

    public Task<OperationResult<UserTwoFactor>> GetPreferredAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult<UserTwoFactor>.Fail("User not authenticated."));

        return _twoFactorClaimService.GetPreferredAsync(userId);
    }

    // ---- ExternalLoginManagementService ----

    public async Task<OperationResult<List<ExternalLoginDto>>> GetConnectedProvidersAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return OperationResult<List<ExternalLoginDto>>.Fail("User not authenticated.");

        var result = await _externalLoginManagementService.GetConnectedProvidersAsync(userId);
        if (!result.Success)
            return OperationResult<List<ExternalLoginDto>>.Fail(result.ErrorMessage ?? "Failed to get connected providers.");

        return OperationResult<List<ExternalLoginDto>>.Ok(result.Data?.ToList() ?? []);
    }

    public Task<OperationResult> DisconnectProviderAsync(string provider)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _externalLoginManagementService.DisconnectProviderAsync(userId, provider);
    }

    public Task<OperationResult> ConnectProviderAsync(ConnectExternalLoginRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(OperationResult.Fail("User not authenticated."));

        return _externalLoginManagementService.ConnectProviderAsync(userId, request);
    }
}
