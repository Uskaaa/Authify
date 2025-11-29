using System.Net;
using System.Net.Http.Headers;
using Authify.Client.Wasm.Interfaces;
using Authify.Client.Wasm.Models;
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

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var storedTokenResult = await _tokenStore.GetRefreshTokenAsync();

            if (storedTokenResult.Success && storedTokenResult.Data != null)
            {
                // Refresh API aufrufen
                var refreshResult = await _authRefreshService.RefreshTokenAsync(storedTokenResult.Data);

                // Prüfen ob wir neue Tokens bekommen haben
                if (refreshResult.Success && !string.IsNullOrEmpty(refreshResult.Data.AccessToken))
                {
                    // Speichern
                    await _tokenStore.SetTokensAsync(refreshResult.Data.AccessToken, refreshResult.Data.RefreshToken);

                    // --- WICHTIG: Request Klonen ---
                    // Man darf einen Request in Blazor oft nicht 2x senden. Wir müssen ihn klonen.
                    var newRequest = await CloneHttpRequestMessageAsync(request);
                    
                    // Neuen Header setzen
                    newRequest.Headers.Authorization = 
                        new AuthenticationHeaderValue("Bearer", refreshResult.Data.AccessToken);
                    
                    // Request wiederholen
                    response = await base.SendAsync(newRequest, cancellationToken);
                }
                else
                {
                    _navManager.NavigateTo("/login", forceLoad: true);
                }
            }
            else
            {
                _navManager.NavigateTo("/login", forceLoad: true);
            }
        }

        return response;
    }

    // Hilfsmethode zum Klonen (unverzichtbar für Retries!)
    private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            foreach (var h in req.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        foreach (var h in req.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        return clone;
    }
}