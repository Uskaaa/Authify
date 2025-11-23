using System.Net.Http.Json;
using System.Text.Json;
using Authify.Core.Common;
using Authify.Core.Models;
using Authify.Core.Server.Models;
using Authify.UI.Services;

namespace Authify.Client.Wasm.Services;

public class WasmDataService : IAuthifyDataService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WasmDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
            return OperationResult<(string AccessToken, string RefreshToken)?>.Fail(loginResult.ErrorMessage ?? "JWT login failed.");
        return OperationResult<(string AccessToken, string RefreshToken)?>.Ok((loginResult.Data.AccessToken!, loginResult.Data.RefreshToken!));
    }

    [System.Obsolete("Use LoginAsync(LoginRequest) returning LoginResponseDto")]
    public async Task<OperationResult<string>> CookieLoginAsync(LoginRequest loginRequest)
    {
        // Cookie-based login is not recommended for WASM clients.
        return await Task.FromResult(OperationResult<string>.Fail("Cookie-based login is not supported in this client."));
    }

    public Task<OperationResult<LoginResponseDto>> LoginAsync(LoginRequest request) => PostAsync<LoginResponseDto>("api/auth/login", request);

    public Task<OperationResult<(string AccessToken, string RefreshToken)?>> JwtVerifyOtpAsync(OtpVerificationRequest request) => PostAsync<(string AccessToken, string RefreshToken)?>("api/auth/verify-otp-jwt", request);

    public Task<OperationResult<string>> CookieVerifyOtpAsync(OtpVerificationRequest request) => PostAsync<string>("api/auth/verify-otp-cookie", request);

    public Task<OperationResult> ResendOtpAsync(ResendOtpRequest request) => PostAsync("api/auth/resend-otp", request);

    public async Task JwtLogoutAsync(string refreshToken) => await PostAsync("api/auth/logout-jwt", new { refreshToken });

    public Task<OperationResult> CookieLogoutAsync() => PostAsync("api/auth/logout-cookie", new { });

    public Task<OperationResult> RegisterAsync(RegisterRequest registerRequest) => PostAsync("api/users/register", registerRequest);

    public Task<OperationResult> ConfirmEmailAsync(EmailConfirmationRequest emailConfirmationRequest) => PostAsync("api/users/confirm-email", emailConfirmationRequest);

    public Task<OperationResult> ForgotPasswordAsync(ForgotPasswordRequest forgotPasswordRequest) => PostAsync("api/users/forgot-password", forgotPasswordRequest);

    public Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest) => PostAsync("api/users/reset-password", resetPasswordRequest);

    public Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request) => PostAsync("api/users/change-password", request);

    public async Task<OperationResult<byte[]>> RequestExportAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/account/export");
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

    public Task<OperationResult<UserExportRequest>> GetExportStatusAsync() => GetAsync<UserExportRequest>("api/account/export-status");

    public Task<OperationResult> DeactivateAccountAsync() => PostAsync("api/account/deactivate", new {});

    public Task<OperationResult<UserDeactivationRequest>> GetDeactivationStatusAsync() => GetAsync<UserDeactivationRequest>("api/account/deactivation-status");

    public Task<OperationResult> DeleteAccountAsync() => PostAsync("api/account/delete", new {});

    public Task<OperationResult<UserDeletionRequest>> GetDeletionStatusAsync() => GetAsync<UserDeletionRequest>("api/account/deletion-status");

    public Task<OperationResult> UpdatePersonalInformationAsync(PersonalInformationUpdateRequest request) => PostAsync("api/profile/personal-information", request);

    public Task<OperationResult> UpdateProfileImageAsync(ProfileImageUpdateRequest request)
    {
        // This requires multipart form data, which is more complex than PostAsJsonAsync
        return Task.FromResult(OperationResult.Fail("Not implemented. Requires multipart form data."));
    }

    public Task<OperationResult<UserProfileDto>> GetProfileAsync() => GetAsync<UserProfileDto>("api/profile");

    public Task<OperationResult> AddOrUpdateAsync(TwoFactorRequest request) => PostAsync("api/twofactor", request);

    public Task<OperationResult> RemoveAsync(TwoFactorRequest request) => PostAsync("api/twofactor/remove", request);

    public Task<OperationResult<List<UserTwoFactor>>> GetAllAsync() => GetAsync<List<UserTwoFactor>>("api/twofactor/all");

    public Task<OperationResult<UserTwoFactor>> GetPreferredAsync() => GetAsync<UserTwoFactor>("api/twofactor/preferred");

    public async Task<OperationResult<List<ExternalLoginDto>>> GetConnectedProvidersAsync()
    {
        try
        {
            var providers = await _httpClient.GetFromJsonAsync<List<ExternalLoginDto>>("api/externallogins", _jsonOptions);
            return providers != null 
                ? OperationResult<List<ExternalLoginDto>>.Ok(providers)
                : OperationResult<List<ExternalLoginDto>>.Fail("Failed to retrieve connected providers.");
        }
        catch (Exception ex)
        {
            return OperationResult<List<ExternalLoginDto>>.Fail(ex.Message);
        }
    }

    public async Task<OperationResult<bool>> CanDisconnectProviderAsync(string provider)
    {
        try
        {
            var canDisconnect = await _httpClient.GetFromJsonAsync<bool>($"api/externallogins/can-disconnect?provider={Uri.EscapeDataString(provider)}");
            return OperationResult<bool>.Ok(canDisconnect);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DisconnectProviderAsync(string provider)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/externallogins/disconnect", new { provider }, _jsonOptions);
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

    public async Task<OperationResult> ConnectProviderAsync(ConnectExternalLoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/externallogins/connect", request, _jsonOptions);
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
}