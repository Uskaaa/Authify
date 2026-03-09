using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Authify.Client.Server.Services;

public class ServerDataService<TUser> : IAuthifyDataService where TUser : ApplicationUser, new()
{
    private readonly IUserService _userService;
    private readonly IUserAccountService _userAccountService;
    private readonly IUserProfileService _userProfileService;
    private readonly ITwoFactorClaimService _twoFactorClaimService;
    private readonly IExternalLoginManagementService _externalLoginManagementService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServerDataService<TUser>> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ServerDataService(
        IUserService userService,
        IUserAccountService userAccountService,
        IUserProfileService userProfileService,
        ITwoFactorClaimService twoFactorClaimService,
        IExternalLoginManagementService externalLoginManagementService,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        ILogger<ServerDataService<TUser>> logger)
    {
        _userService = userService;
        _userAccountService = userAccountService;
        _userProfileService = userProfileService;
        _twoFactorClaimService = twoFactorClaimService;
        _externalLoginManagementService = externalLoginManagementService;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // ---- HTTP proxy helpers for cookie-setting operations ----

    /// <summary>
    /// Creates an HttpClient for same-server calls. UseCookies is disabled so that
    /// Set-Cookie headers are accessible on the response and can be forwarded to the browser.
    /// </summary>
    private HttpClient CreateLocalClient()
    {
        var httpCtx = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is unavailable.");

        var client = _httpClientFactory.CreateClient("AuthifyServerLocal");
        client.BaseAddress = new Uri($"{httpCtx.Request.Scheme}://{httpCtx.Request.Host}");

        // Forward the caller's cookies so the controller has full auth context if needed
        var existingCookies = httpCtx.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrEmpty(existingCookies))
        {
            client.DefaultRequestHeaders.Remove("Cookie");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", existingCookies);
        }

        return client;
    }

    /// <summary>
    /// Copies Set-Cookie headers from an internal HTTP response back to the browser's response.
    /// This only works when the current HTTP response has not yet started (i.e. SSR / form-POST context).
    /// In Interactive Server mode the WebSocket handshake has already been sent, so headers are skipped.
    /// </summary>
    private void ForwardSetCookieHeaders(HttpResponseMessage internalResponse)
    {
        var httpCtx = _httpContextAccessor.HttpContext;
        if (httpCtx is null || httpCtx.Response.HasStarted)
            return;

        if (internalResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
                httpCtx.Response.Headers.Append("Set-Cookie", cookie);
        }
    }

    private async Task<OperationResult<T>> PostToAuthEndpointAsync<T>(string path, object body)
    {
        try
        {
            using var client = CreateLocalClient();
            var response = await client.PostAsJsonAsync(path, body, JsonOptions);
            ForwardSetCookieHeaders(response);

            var result = await response.Content.ReadFromJsonAsync<OperationResult<T>>(JsonOptions);
            return result ?? OperationResult<T>.Fail("Failed to deserialize auth response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling auth endpoint {Path}", path);
            return OperationResult<T>.Fail(ex.Message);
        }
    }

    private async Task<OperationResult> PostToAuthEndpointAsync(string path, object? body = null)
    {
        try
        {
            using var client = CreateLocalClient();
            var response = body is null
                ? await client.PostAsync(path, content: null)
                : await client.PostAsJsonAsync(path, body, JsonOptions);
            ForwardSetCookieHeaders(response);

            var result = await response.Content.ReadFromJsonAsync<OperationResult>(JsonOptions);
            return result ?? OperationResult.Fail("Failed to deserialize auth response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling auth endpoint {Path}", path);
            return OperationResult.Fail(ex.Message);
        }
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

    /// <summary>
    /// Delegates to <see cref="CookieAuthController"/> via a real HTTP POST so that
    /// ASP.NET Core Identity's SignInManager can write the HttpOnly auth cookie
    /// to the HTTP response (required – cookies cannot be set over a WebSocket).
    /// </summary>
    public Task<OperationResult<LoginResponseDto>> LoginAsync(LoginRequest request)
        => PostToAuthEndpointAsync<LoginResponseDto>("api/server-auth/login", request);

    /// <summary>
    /// Delegates OTP verification to <see cref="CookieAuthController"/> via HTTP so the
    /// auth cookie is written to a real HTTP response.
    /// </summary>
    public Task<OperationResult<OtpResponseDto>> JwtVerifyOtpAsync(OtpVerificationRequest request)
        => PostToAuthEndpointAsync<OtpResponseDto>("api/server-auth/verify-otp", request);

    public Task<OperationResult<string>> CookieVerifyOtpAsync(OtpVerificationRequest request)
        => PostToAuthEndpointAsync<string>("api/server-auth/verify-otp", request);

    public Task<OperationResult> ResendOtpAsync(ResendOtpRequest request)
        => PostToAuthEndpointAsync("api/server-auth/resend-otp", request);

    /// <summary>Cookie-based logout via HTTP so Identity can clear the HttpOnly cookie.</summary>
    public Task<OperationResult> JwtLogoutAsync()
        => PostToAuthEndpointAsync("api/server-auth/logout");

    public Task<OperationResult> CookieLogoutAsync()
        => PostToAuthEndpointAsync("api/server-auth/logout");

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
