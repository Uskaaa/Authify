using System.Net.Http.Json;
using Authify.Client.Wasm.Interfaces;
using Authify.Client.Wasm.Models;
using Authify.Core.Common;

namespace Authify.Client.Wasm.Services;

public class AuthRefreshService : IAuthRefreshService
{
    private readonly HttpClient _httpClient;

    public AuthRefreshService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OperationResult<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Der Post-Request bleibt gleich
        var response = await _httpClient.PostAsJsonAsync("api/RefreshToken/renew", request);

        if (!response.IsSuccessStatusCode)
        {
            return OperationResult<(string, string)>.Fail("Failed to refresh token.");
        }

        var data = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        
        if (data == null || string.IsNullOrEmpty(data.RefreshToken))
            return OperationResult<(string, string)>.Fail("Invalid response.");
        
        // Wir geben die rohen Strings zurück
        return OperationResult<(string, string)>.Ok((data.AccessToken, data.RefreshToken));
    }
}