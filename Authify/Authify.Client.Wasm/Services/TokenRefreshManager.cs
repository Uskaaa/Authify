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
            var currentAccessToken = await _tokenStore.GetAccessTokenAsync();

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
}