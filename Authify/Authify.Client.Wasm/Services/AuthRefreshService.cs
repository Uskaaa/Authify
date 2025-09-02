using System.Net.Http.Json;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;

namespace Authify.Client.Wasm.Services;

public class AuthRefreshService : IAuthRefreshService
{
    private readonly HttpClient _httpClient;

    public AuthRefreshService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OperationResult<(string AccessToken, RefreshTokenRequest RefreshToken)>> RefreshTokenAsync(RefreshTokenRequest request)
    {

        var response = await _httpClient.PostAsJsonAsync("api/RefreshToken/renew", request);

        if (!response.IsSuccessStatusCode)
        {
            return OperationResult<(string, RefreshTokenRequest)>.Fail("Failed to refresh token.");
        }

        var data = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        if (data == null)
            return OperationResult<(string, RefreshTokenRequest)>.Fail("Invalid response.");
        
        return OperationResult<(string, RefreshTokenRequest)>.Ok((data.AccessToken, data.RefreshToken));
    }
}