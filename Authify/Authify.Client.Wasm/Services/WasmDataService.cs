using System.Net.Http.Json;
using System.Text.Json;
using Authify.Client.Wasm.Extensions;
using Authify.Client.Wasm.Interfaces;
using Authify.Core.Common;
using Authify.Core.Models;
using Authify.UI.Services;

namespace Authify.Client.Wasm.Services;

public class WasmDataService : IAuthifyDataService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ITokenStore _tokenStore;

    public WasmDataService(HttpClient httpClient, ITokenStore tokenStore)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
    }
    
    #region Helpers

    private async Task<OperationResult<T>> PostAsync<T>(string url, object payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OperationResult<T>>(_jsonOptions);
                return result ?? OperationResult<T>.Fail("Failed to deserialize successful response.");
            }

            // Try to read OperationResult from error response
            var errorResult = await response.Content.ReadFromJsonAsync<OperationResult<T>>(_jsonOptions);
            return errorResult ?? OperationResult<T>.Fail(await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            return OperationResult<T>.Fail(ex.Message);
        }
    }

    private async Task<OperationResult> PostAsync(string url, object payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OperationResult>(_jsonOptions);
                return result ?? OperationResult.Fail("Failed to deserialize successful response.");
            }

            var errorResult = await response.Content.ReadFromJsonAsync<OperationResult>(_jsonOptions);
            return errorResult ?? OperationResult.Fail(await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }

    private async Task<OperationResult<T>> GetAsync<T>(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OperationResult<T>>(_jsonOptions);
                return result ?? OperationResult<T>.Fail("Failed to deserialize successful response.");
            }

            var errorResult = await response.Content.ReadFromJsonAsync<OperationResult<T>>(_jsonOptions);
            return errorResult ?? OperationResult<T>.Fail(await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            return OperationResult<T>.Fail(ex.Message);
        }
    }

    private async Task<OperationResult> GetAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OperationResult>(_jsonOptions);
                return result ?? OperationResult.Fail("Failed to deserialize successful response.");
            }

            var errorResult = await response.Content.ReadFromJsonAsync<OperationResult>(_jsonOptions);
            return errorResult ?? OperationResult.Fail(await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }

    #endregion

    [System.Obsolete("Use LoginAsync(LoginRequest) returning LoginResponseDto")]
    public async Task<OperationResult<(string AccessToken, string RefreshToken)?>> JwtLoginAsync(LoginRequest request)
    {
        var loginResult = await LoginAsync(request);
        if (!loginResult.Success || loginResult.Data?.ResultKind != LoginResultKind.Jwt)
            return OperationResult<(string AccessToken, string RefreshToken)?>.Fail(loginResult.ErrorMessage ??
                "JWT login failed.");
        return OperationResult<(string AccessToken, string RefreshToken)?>.Ok((loginResult.Data.AccessToken!,
            loginResult.Data.RefreshToken!));
    }

    [System.Obsolete("Use LoginAsync(LoginRequest) returning LoginResponseDto")]
    public async Task<OperationResult<string>> CookieLoginAsync(LoginRequest loginRequest)
    {
        // Cookie-based login is not recommended for WASM clients.
        return await Task.FromResult(
            OperationResult<string>.Fail("Cookie-based login is not supported in this client."));
    }

    public async Task<OperationResult<LoginResponseDto>> LoginAsync(LoginRequest request)
    {
        // 1. Request senden
        var result = await PostAsync<LoginResponseDto>("api/auth/login", request);
        // 2. Prüfen ob erfolgreich
        if (result.Success && result.Data != null && !string.IsNullOrEmpty(result.Data.RefreshToken))
            await _tokenStore.SetTokensAsync(result.Data.AccessToken, result.Data.RefreshToken);

        return result;
    }

    public async Task<OperationResult<OtpResponseDto>> JwtVerifyOtpAsync(
        OtpVerificationRequest request)
    {
        var result = await PostAsync<OtpResponseDto>("api/auth/verifyotp", request);

        if (result.Success && result.Data != null && !string.IsNullOrEmpty(result.Data.RefreshToken))
            await _tokenStore.SetTokensAsync(result.Data.AccessToken, result.Data.RefreshToken);

        return result;
    }

    public Task<OperationResult<string>> CookieVerifyOtpAsync(OtpVerificationRequest request) =>
        PostAsync<string>("api/auth/verify-otp-cookie", request);

    public Task<OperationResult> ResendOtpAsync(ResendOtpRequest request) => PostAsync("api/auth/resendotp", request);

    public async Task<OperationResult> JwtLogoutAsync()
    {
        // 1. Refresh Token holen, um ihn an die API zu senden
        var tokenResult = await _tokenStore.GetRefreshTokenAsync();

        if (tokenResult.Success && tokenResult.Data != null)
        {
            await PostAsync("api/auth/logout", tokenResult.Data.RefreshToken);
        }

        await _tokenStore.RemoveTokensAsync();

        return OperationResult.Ok();
    }

    public Task<OperationResult> CookieLogoutAsync() => PostAsync("api/auth/logout-cookie", new { });

    public async Task<string?> GetAccessTokenAsync()
    {
        var result = await _tokenStore.GetAccessTokenAsync();
        return result;
    }
    
    public async Task<OperationResult> StoreTokensFromExternalAuth(string accessToken, string refreshToken)
    {
        await _tokenStore.SetTokensAsync(accessToken, refreshToken);
        return OperationResult.Ok();
    }

    public Task<OperationResult> RegisterAsync(RegisterRequest registerRequest) =>
        PostAsync("api/user/register", registerRequest);

    public Task<OperationResult> ConfirmEmailAsync(EmailConfirmationRequest emailConfirmationRequest) =>
        PostAsync("api/user/confirmemail", emailConfirmationRequest);

    public Task<OperationResult> ForgotPasswordAsync(ForgotPasswordRequest forgotPasswordRequest) =>
        PostAsync("api/user/forgotpassword", forgotPasswordRequest);

    public Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest) =>
        PostAsync("api/user/resetpassword", resetPasswordRequest);

    public Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request) =>
        PostAsync("api/user/changepassword", request);

    public async Task<OperationResult<byte[]>> RequestExportAsync()
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/useraccount/export", new { });
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                return OperationResult<byte[]>.Ok(bytes);
            }

            return OperationResult<byte[]>.Fail(await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            return OperationResult<byte[]>.Fail(ex.Message);
        }
    }

    public Task<OperationResult<UserExportRequest>> GetExportStatusAsync() =>
        GetAsync<UserExportRequest>("api/useraccount/export/status");

    public Task<OperationResult> DeactivateAccountAsync() => PostAsync("api/useraccount/deactivate", new { });

    public Task<OperationResult<UserDeactivationRequest>> GetDeactivationStatusAsync() =>
        GetAsync<UserDeactivationRequest>("api/useraccount/deactivate/status");

    public Task<OperationResult> DeleteAccountAsync() => PostAsync("api/useraccount/delete", new { });

    public Task<OperationResult<UserDeletionRequest>> GetDeletionStatusAsync() =>
        GetAsync<UserDeletionRequest>("api/useraccount/delete/status");

    public Task<OperationResult> UpdatePersonalInformationAsync(PersonalInformationUpdateRequest request) =>
        PostAsync("api/userprofile/update-personal-information", request);

    public Task<OperationResult> UpdateProfileImageAsync(ProfileImageUpdateRequest request) =>
        PostAsync("api/userprofile/update-profile-image", request);

    public Task<OperationResult<UserProfileDto>> GetProfileAsync() => GetAsync<UserProfileDto>("api/userprofile/me");

    public Task<OperationResult> SendPhoneVerificationCodeAsync() =>
        PostAsync("api/userprofile/send-phone-verification", new { });

    public Task<OperationResult> VerifyPhoneNumberAsync(string code) =>
        PostAsync("api/userprofile/verify-phone", new { Code = code });


    public Task<OperationResult> AddOrUpdateAsync(TwoFactorRequest request) =>
        PostAsync("api/twofactorclaim/add-or-update", request);

    public Task<OperationResult> RemoveAsync(TwoFactorRequest request) =>
        PostAsync("api/twofactorclaim/remove", request);

    public Task<OperationResult<List<UserTwoFactor>>> GetAllAsync() =>
        GetAsync<List<UserTwoFactor>>("api/twofactorclaim/all");

    public Task<OperationResult<UserTwoFactor>> GetPreferredAsync() =>
        GetAsync<UserTwoFactor>("api/twofactorclaim/preferred");

    public async Task<OperationResult<List<ExternalLoginDto>>> GetConnectedProvidersAsync() =>
        await GetAsync<List<ExternalLoginDto>>("api/externallogin/connected");

    public async Task<OperationResult> DisconnectProviderAsync(string provider)
    {
        try
        {
            var response =
                await _httpClient.PostAsJsonAsync($"api/externallogin/disconnect/{provider}", new { }, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OperationResult>(_jsonOptions);
                return result ?? OperationResult.Ok();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return OperationResult.Fail(errorContent);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> ConnectProviderAsync(ConnectExternalLoginRequest request) =>
        await PostAsync("api/externallogin/connect", request);

    public Task<OperationResult<CreatePersonalAccessTokenResponse>> CreatePersonalAccessTokenAsync(CreatePersonalAccessTokenRequest request) =>
        PostAsync<CreatePersonalAccessTokenResponse>("api/personalaccesstoken/create", request);

    public Task<OperationResult<List<PersonalAccessTokenDto>>> GetPersonalAccessTokensAsync() =>
        GetAsync<List<PersonalAccessTokenDto>>("api/personalaccesstoken/mine");

    public Task<OperationResult> RevokePersonalAccessTokenAsync(Guid id) =>
        PostAsync($"api/personalaccesstoken/revoke/{id}", new { });

    public Task<OperationResult<ResolvePersonalAccessTokenResponse>> ResolvePersonalAccessTokenAsync(string token) =>
        GetAsync<ResolvePersonalAccessTokenResponse>($"api/personalaccesstoken/resolve?token={Uri.EscapeDataString(token)}");
}
