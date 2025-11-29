using System.Text.Json;
using Authify.Client.Wasm.Interfaces;
using Authify.Client.Wasm.Models;
using Authify.UI.Common;
using Microsoft.JSInterop;

namespace Authify.Client.Wasm.Services;

public class TokenStore : ITokenStore
{
    private readonly IJSRuntime _jsRuntime;
    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";

    public TokenStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
    }

    public async Task<OperationResult<RefreshTokenRequest>> GetRefreshTokenAsync()
    {
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
        if (string.IsNullOrEmpty(json))
            return OperationResult<RefreshTokenRequest>.Fail("No refresh token found.");

        try
        {
            var refreshToken = JsonSerializer.Deserialize<RefreshTokenRequest>(json);
            return OperationResult<RefreshTokenRequest>.Ok(refreshToken!);
        }
        catch (Exception ex)
        {
            return OperationResult<RefreshTokenRequest>.Fail($"Failed to deserialize refresh token: {ex.Message}");
        }
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
    }
    
    public async Task RemoveTokensAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
    }
}