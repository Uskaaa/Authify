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

    // Standard-Zustand: Nicht eingeloggt (Anonym)
    private readonly AuthenticationState _anonymous;

    public JwtAuthenticationStateProvider(ITokenStore tokenStore, HttpClient httpClient, NavigationManager navManager, IAuthifyDataService dataService)
    {
        _tokenStore = tokenStore;
        _httpClient = httpClient;
        _navManager = navManager;
        _dataService = dataService;
        _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        
        _dataService.OnLoggedOut += HandleLogoutEvent;
    }

    public void Dispose()
    {
        if (_dataService != null)
        {
            _dataService.OnLoggedOut -= HandleLogoutEvent;
        }
    }
    
    private void HandleLogoutEvent()
    {
        // 1. Header entfernen
        _httpClient.DefaultRequestHeaders.Authorization = null;

        // 2. UI Status auf "Anonym" setzen
        var authState = Task.FromResult(_anonymous);
        NotifyAuthenticationStateChanged(authState);

        // 3. Navigation durchführen
        _navManager.NavigateTo("/login");
    }
    
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // 1. Token aus LocalStorage holen
            var token = await _tokenStore.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                return _anonymous;
            }

            // 2. Token parsen (Claims extrahieren)
            // HINWEIS: Wir prüfen hier NICHT das Ablaufdatum (exp). 
            // Das ist "Strategie A": Wir sind optimistisch.
            var claims = ParseClaimsFromJwt(token);

            // 3. User Identität erstellen
            // Der String "jwt" als zweiter Parameter ist extrem wichtig! 
            // Ohne ihn ist User.Identity.IsAuthenticated = false.
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            // Wenn Token korrupt ist oder Parsing fehlschlägt -> Logout
            return _anonymous;
        }
    }

    // --- Manuelle Methoden für Login/Logout (ruft AuthService auf) ---
    public void NotifyUserAuthentication(string token)
    {
        var authenticatedUser = new ClaimsPrincipal(
            new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
            
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        
        // Token auch sofort im HttpClient setzen
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Blazor UI informieren, dass sich der Status geändert hat
        NotifyAuthenticationStateChanged(authState);
    }

    // --- Hilfsmethoden zum Parsen des JWT ---

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
                // Wichtig: Rollen können als Array kommen ["Admin", "User"]
                // Wir müssen sie in einzelne Claims aufteilen.
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