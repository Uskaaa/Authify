using System.Net;
using System.Net.Http.Headers;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Authify.Client.Wasm.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly IAuthRefreshService _authRefreshService;
    private readonly ITokenStore _tokenStore; // Interface für Access + Refresh Token Speicherung
    private readonly NavigationManager _navManager;

    public AuthenticatedHttpClientHandler(IAuthRefreshService authRefreshService, ITokenStore tokenStore, NavigationManager navManager)
    {
        _authRefreshService = authRefreshService;
        _tokenStore = tokenStore;
        _navManager = navManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // JWT anhängen
        var accessToken = await _tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);

        // 401 = Token abgelaufen
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refreshToken = await _tokenStore.GetRefreshTokenAsync();

            // RefreshToken an AuthRefreshService übergeben
            if (refreshToken.Data != null)
            {
                var refreshResult = await _authRefreshService.RefreshTokenAsync(refreshToken.Data);
                if (refreshResult.Success && !string.IsNullOrEmpty(refreshResult.Data.AccessToken))
                {
                    // Tokens speichern
                    await _tokenStore.SetTokensAsync(refreshResult.Data.AccessToken, refreshResult.Data.RefreshToken);

                    // Ursprüngliche Anfrage erneut senden mit neuem JWT
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", refreshResult.Data.AccessToken);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }
            else
            {
                // RefreshToken ungültig → Redirect
                _navManager.NavigateTo("/login", forceLoad: true);
                response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        }

        return response;
    }
}