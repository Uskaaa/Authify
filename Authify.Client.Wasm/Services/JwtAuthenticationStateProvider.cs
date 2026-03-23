using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Authify.Client.Wasm.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Authify.Client.Wasm.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly ITokenStore _tokenStore;
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navManager;
    private readonly TokenRefreshManager _refreshManager;

    private readonly AuthenticationState _anonymous;

    public JwtAuthenticationStateProvider(
        ITokenStore tokenStore, 
        HttpClient httpClient, 
        NavigationManager navManager, 
        TokenRefreshManager refreshManager)
    {
        _tokenStore = tokenStore;
        _httpClient = httpClient;
        _navManager = navManager;
        _refreshManager = refreshManager;
        _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public void Dispose() { }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var accessToken = await _tokenStore.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(accessToken))
                return _anonymous;

            var claims = ParseClaimsFromJwt(accessToken).ToList();

            if (IsTokenExpired(claims))
            {
                Console.WriteLine("[AuthState] Token expired. Calling RefreshManager...");

                var newAccessToken = await _refreshManager.TryRefreshTokenAsync();
                
                if (!string.IsNullOrEmpty(newAccessToken))
                {
                    accessToken = newAccessToken;
                    claims = ParseClaimsFromJwt(accessToken).ToList();
                }
                else
                {
                    Console.WriteLine("[AuthState] Refresh failed. Logging out.");
                    await _tokenStore.RemoveTokensAsync();
                    NotifyUserLogout(); 
                    _navManager.NavigateTo("/login");
                    return _anonymous;
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthState] Error: {ex.Message}");
            return _anonymous;
        }
    }
    
    public async Task<bool> ForceRefreshTokenAsync()
    {
        // Auch hier: Manager nutzen
        var newToken = await _refreshManager.TryRefreshTokenAsync();
    
        if (!string.IsNullOrEmpty(newToken))
        {
            NotifyTokenUpdated(newToken);
            return true;
        }
        return false;
    }

    private bool IsTokenExpired(IEnumerable<Claim> claims)
    {
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim == null) return false;

        if (long.TryParse(expClaim.Value, out long expSeconds))
        {
            var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
            if (expDate <= DateTime.UtcNow.AddSeconds(10)) return true; 
        }
        return false;
    }
    
    public void NotifyUserLogout()
    {
        var authState = Task.FromResult(_anonymous);
        _httpClient.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(authState);
    }
    
    public void NotifyTokenUpdated(string newToken)
    {
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(newToken), "jwt"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        NotifyAuthenticationStateChanged(authState);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray()) claims.Add(new Claim(kvp.Key, item.ToString()));
                }
                else claims.Add(new Claim(kvp.Key, kvp.Value.ToString() ?? ""));
            }
        }
        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}