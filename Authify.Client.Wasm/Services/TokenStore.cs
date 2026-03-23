using System.Text.Json;
using Authify.Client.Wasm.Interfaces;
using Authify.Core.Models;
using Authify.Core.Common;
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
        var tokenString = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);

        if (string.IsNullOrEmpty(tokenString))
            return OperationResult<RefreshTokenRequest>.Fail("No refresh token found.");
        
        var request = new RefreshTokenRequest
        {
            RefreshToken = tokenString,
            DeviceName = "WebApp",
            IpAddress = "Unknown",
        };

        return OperationResult<RefreshTokenRequest>.Ok(request);
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