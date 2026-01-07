using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Authify.Client.Wasm.Interfaces;
using Authify.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Authify.Client.Wasm.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly ITokenStore _tokenStore;
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navManager;
    private readonly IAuthifyDataService _dataService;
    private readonly IAuthRefreshService _authRefreshService;

    private readonly AuthenticationState _anonymous;

    public JwtAuthenticationStateProvider(
        ITokenStore tokenStore, 
        HttpClient httpClient, 
        NavigationManager navManager, 
        IAuthifyDataService dataService,
        IAuthRefreshService authRefreshService)
    {
        _tokenStore = tokenStore;
        _httpClient = httpClient;
        _navManager = navManager;
        _dataService = dataService;
        _authRefreshService = authRefreshService;
        _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public void Dispose()
    {
        
    }
    
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Access Token holen
            var accessToken = await _tokenStore.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return _anonymous;
            }

            // Claims parsen
            var claims = ParseClaimsFromJwt(accessToken).ToList();

            // Prüfen ob abgelaufen
            if (IsTokenExpired(claims))
            {
                Console.WriteLine("[AuthState] Token expired. Attempting silent refresh...");
                
                // Versuchen zu refreshen
                var refreshResult = await TryRefreshTokenAsync();
                
                if (!string.IsNullOrEmpty(refreshResult))
                {
                    // Refresh erfolgreich -> Neuen Token nutzen
                    accessToken = refreshResult;
                    claims = ParseClaimsFromJwt(accessToken).ToList();
                }
                else
                {
                    Console.WriteLine("[AuthState] Silent refresh failed. Logging out.");
                    await _tokenStore.RemoveTokensAsync();
                    
                    NotifyUserLogout(); 
                    
                    _navManager.NavigateTo("/login");
                    
                    return _anonymous;
                }
            }

            // User Identität erstellen (Alles valid)
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            // Token im HttpClient setzen
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
        var newToken = await TryRefreshTokenAsync();
    
        if (!string.IsNullOrEmpty(newToken))
        {
            NotifyTokenUpdated(newToken);
            return true;
        }
    
        return false;
    }
    
    private async Task<string?> TryRefreshTokenAsync()
    {
        try 
        {
            var storedTokenResult = await _tokenStore.GetRefreshTokenAsync();
            if (!storedTokenResult.Success || storedTokenResult.Data == null)
                return null;

            var result = await _authRefreshService.RefreshTokenAsync(storedTokenResult.Data);
            
            if (result.Success && !string.IsNullOrEmpty(result.Data.AccessToken))
            {
                // Neue Tokens speichern
                await _tokenStore.SetTokensAsync(result.Data.AccessToken, result.Data.RefreshToken);
                return result.Data.AccessToken;
            }
        }
        catch
        {
            // Fehler beim Refresh ignorieren -> führt zu Logout
        }
        
        return null;
    }

    private bool IsTokenExpired(IEnumerable<Claim> claims)
    {
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim == null) return false;

        // JWT exp ist Sekunden seit Unix Epoch
        if (long.TryParse(expClaim.Value, out long expSeconds))
        {
            var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;

            if (expDate <= DateTime.UtcNow.AddSeconds(10))
            {
                return true; 
            }
        }
        return false;
    }
    
    public void NotifyUserAuthentication(string token)
    {
        var authenticatedUser = new ClaimsPrincipal(
            new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
            
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        NotifyAuthenticationStateChanged(authState);
    }
    
    public void NotifyUserLogout()
    {
        var authState = Task.FromResult(_anonymous);
        _httpClient.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(authState);
    }
    
    public void NotifyTokenUpdated(string newToken)
    {
        var authenticatedUser = new ClaimsPrincipal(
            new ClaimsIdentity(ParseClaimsFromJwt(newToken), "jwt"));
            
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", newToken);

        NotifyAuthenticationStateChanged(authState);
    }

    // --- Parsing ---
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
                    foreach (var item in element.EnumerateArray())
                    {
                        claims.Add(new Claim(kvp.Key, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString() ?? ""));
                }
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