using System.Text.Json;
using Authify.Client.Wasm.Interfaces;

namespace Authify.Client.Wasm.Services;

public class TokenRefreshManager
{
    private readonly IAuthRefreshService _refreshService;
    private readonly ITokenStore _tokenStore;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public TokenRefreshManager(IAuthRefreshService refreshService, ITokenStore tokenStore)
    {
        _refreshService = refreshService;
        _tokenStore = tokenStore;
    }

    public async Task<string?> TryRefreshTokenAsync()
    {
        await _semaphore.WaitAsync();
        
        try
        {
            var currentToken = await _tokenStore.GetAccessTokenAsync();

            if (!string.IsNullOrEmpty(currentToken) && !IsTokenExpired(currentToken))
            {
                return currentToken;
            }

            // --- Refresh durchführen ---

            var storedRefreshTokenResult = await _tokenStore.GetRefreshTokenAsync();
            
            if (!storedRefreshTokenResult.Success || storedRefreshTokenResult.Data == null)
            {
                return null;
            }

            var result = await _refreshService.RefreshTokenAsync(storedRefreshTokenResult.Data);

            if (result.Success && !string.IsNullOrEmpty(result.Data.AccessToken))
            {
                await _tokenStore.SetTokensAsync(result.Data.AccessToken, result.Data.RefreshToken);
                return result.Data.AccessToken;
            }

            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var payload = token.Split('.')[1];
            // Base64 Fix (Padding)
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var jsonBytes = Convert.FromBase64String(payload);
            var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (claims != null && claims.TryGetValue("exp", out var expObj))
            {
                if (long.TryParse(expObj.ToString(), out long expSeconds))
                {
                    var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                    if (expDate > DateTime.UtcNow.AddSeconds(10))
                    {
                        return false; 
                    }
                }
            }
            return true;
        }
        catch
        {
            return true;
        }
    }
}