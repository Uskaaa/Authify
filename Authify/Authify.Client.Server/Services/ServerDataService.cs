using Authify.UI.Common;
using Authify.Core.Interfaces;
using Authify.UI.Models;
using Authify.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Authify.Application.Data;

namespace Authify.Client.Server.Services;

public class ServerDataService<TUser> : IAuthifyDataService where TUser : ApplicationUser, new()
{
    private readonly IAuthServiceCookie _authServiceCookie;
    private readonly IAuthServiceJwt _authServiceJwt;
    private readonly IUserService _userService;
    private readonly IUserAccountService _userAccountService;
    private readonly IUserProfileService _userProfileService;
    private readonly ITwoFactorClaimService _twoFactorClaimService;
    private readonly IExternalLoginManagementService<TUser> _externalLoginManagementService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<TUser> _userManager;

    public ServerDataService(IAuthServiceCookie authServiceCookie,
        IAuthServiceJwt authServiceJwt,
        IUserService userService,
        IUserAccountService userAccountService,
        IUserProfileService userProfileService,
        ITwoFactorClaimService twoFactorClaimService,
        IExternalLoginManagementService<TUser> externalLoginManagementService,
        IHttpContextAccessor httpContextAccessor,
        UserManager<TUser> userManager)
    {
        _authServiceCookie = authServiceCookie;
        _authServiceJwt = authServiceJwt;
        _userService = userService;
        _userAccountService = userAccountService;
        _userProfileService = userProfileService;
        _twoFactorClaimService = twoFactorClaimService;
        _externalLoginManagementService = externalLoginManagementService;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [System.Obsolete("Use LoginAsync(LoginRequest) returning LoginResponseDto")]
    public async Task<OperationResult<(string AccessToken, string RefreshToken)?>> JwtLoginAsync(LoginRequest request)
    {
        var loginResult = await LoginAsync(request);
        if (!loginResult.Success || loginResult.Data?.ResultKind != LoginResultKind.Jwt)
            return OperationResult<(string AccessToken, string RefreshToken)?>.Fail(loginResult.ErrorMessage ?? "JWT login failed.");

        return OperationResult<(string AccessToken, string RefreshToken)?>.Ok((loginResult.Data.AccessToken!, loginResult.Data.RefreshToken!));
    }

    [System.Obsolete("Use LoginAsync(LoginRequest) returning LoginResponseDto")]
    public async Task<OperationResult<string>> CookieLoginAsync(LoginRequest loginRequest)
    {
        var loginResult = await LoginAsync(loginRequest);
        if (!loginResult.Success || loginResult.Data?.ResultKind != LoginResultKind.Cookie)
            return OperationResult<string>.Fail(loginResult.ErrorMessage ?? "Cookie login failed.");

        return OperationResult<string>.Ok(loginResult.Data.CookieName ?? string.Empty);
    }

    public async Task<OperationResult<LoginResponseDto>> LoginAsync(LoginRequest request)
    {
        // For server-side, we can decide which service to use based on configuration or a parameter.
        // Let's assume for now we try cookie auth by default if running in a server context.
        // A better approach might be to have a flag in the request or configuration.

        var http = _httpContextAccessor.HttpContext;
        if (http != null)
        {
            request.DeviceName ??= http.Request.Headers["Device-Name"].FirstOrDefault() ?? "Unknown";
            request.IpAddress ??= http.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        // Using Cookie Auth Service
        var cookieResult = await _authServiceCookie.LoginAsync(request);
        if (cookieResult.Success)
        {
            if (string.IsNullOrEmpty(cookieResult.Data)) // Cookie was set
            {
                return OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
                {
                    ResultKind = LoginResultKind.Cookie,
                    CookieSet = true,
                    Message = "Cookie authentication succeeded."
                });
            }
            else // OTP is required - token is in Data
            {
                return OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
                {
                    ResultKind = LoginResultKind.Cookie,
                    Message = "OTP required.",
                    AccessToken = cookieResult.Data // Temporary token for OTP verification
                });
            }
        }

        // Fallback or explicit JWT auth
        var jwtResult = await _authServiceJwt.LoginAsync(request);
        if (jwtResult.Success)
        {
            var (accessToken, refreshToken) = jwtResult.Data!.Value;
            if (string.IsNullOrEmpty(refreshToken)) // OTP required for JWT
            {
                return OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
                {
                    ResultKind = LoginResultKind.Jwt,
                    Message = "OTP required.",
                    AccessToken = accessToken // This is the OTP token
                });
            }
            return OperationResult<LoginResponseDto>.Ok(new LoginResponseDto
            {
                ResultKind = LoginResultKind.Jwt,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        return OperationResult<LoginResponseDto>.Fail(jwtResult.ErrorMessage ?? cookieResult.ErrorMessage ?? "Login failed.");
    }

    public Task<OperationResult<(string AccessToken, string RefreshToken)?>> JwtVerifyOtpAsync(OtpVerificationRequest request)
        => _authServiceJwt.VerifyOtpAsync(request);

    public async Task<OperationResult<string>> CookieVerifyOtpAsync(OtpVerificationRequest request)
    {
        var result = await _authServiceCookie.VerifyOtpAsync(request);
        return result.Success ? OperationResult<string>.Ok("Cookie set successfully.") : OperationResult<string>.Fail(result.ErrorMessage ?? "OTP verification failed.");
    }

    public Task<OperationResult> ResendOtpAsync(ResendOtpRequest request)
    {
        // We need to know whether to use the JWT or Cookie service path.
        // This could be based on the token format or an additional parameter.
        // Assuming JWT for now as it's more likely to be stateless.
        return _authServiceJwt.ResendOtpAsync(request);
    }

    public Task JwtLogoutAsync(string refreshToken) => _authServiceJwt.LogoutAsync(refreshToken);

    public Task<OperationResult> CookieLogoutAsync() => _authServiceCookie.LogoutAsync();

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

    public async Task<OperationResult<List<ExternalLoginDto>>> GetConnectedProvidersAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return OperationResult<List<ExternalLoginDto>>.Fail("User not authenticated.");
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult<List<ExternalLoginDto>>.Fail("User not found.");
        
        var providers = await _externalLoginManagementService.GetConnectedProvidersAsync(user);
        
        // Convert UserLoginInfo to ExternalLoginDto
        var dtos = providers.Select(p => new ExternalLoginDto
        {
            LoginProvider = p.LoginProvider,
            ProviderKey = p.ProviderKey,
            ProviderDisplayName = p.ProviderDisplayName
        }).ToList();
        
        return OperationResult<List<ExternalLoginDto>>.Ok(dtos);
    }

    public async Task<OperationResult<bool>> CanDisconnectProviderAsync(string provider)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return OperationResult<bool>.Fail("User not authenticated.");
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult<bool>.Fail("User not found.");
        
        var canDisconnect = await _externalLoginManagementService.CanDisconnectProviderAsync(user, provider);
        return OperationResult<bool>.Ok(canDisconnect);
    }

    public async Task<OperationResult> DisconnectProviderAsync(string provider)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return OperationResult.Fail("User not authenticated.");
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");
        
        var result = await _externalLoginManagementService.DisconnectProviderAsync(user, provider);
        
        if (result.Succeeded)
            return OperationResult.Ok();
        
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return OperationResult.Fail(errors);
    }

    public async Task<OperationResult> ConnectProviderAsync(ConnectExternalLoginRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return OperationResult.Fail("User not authenticated.");
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");
        
        // Convert ConnectExternalLoginRequest to UserLoginInfo
        var loginInfo = new UserLoginInfo(
            request.LoginProvider,
            request.ProviderKey,
            request.ProviderDisplayName
        );
        
        var result = await _externalLoginManagementService.ConnectProviderAsync(user, loginInfo);
        
        if (result.Succeeded)
            return OperationResult.Ok();
        
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return OperationResult.Fail(errors);
    }
}