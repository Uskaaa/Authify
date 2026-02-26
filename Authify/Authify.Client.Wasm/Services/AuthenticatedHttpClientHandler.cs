using System.Net;
using System.Net.Http.Headers;
using Authify.Client.Wasm.Interfaces;
using Microsoft.AspNetCore.Components;


namespace Authify.Client.Wasm.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly TokenRefreshManager _refreshManager;
    private readonly ITokenStore _tokenStore;
    private readonly NavigationManager _navManager;

    public AuthenticatedHttpClientHandler(TokenRefreshManager refreshManager,
        ITokenStore tokenStore, NavigationManager navManager)
    {
        _refreshManager = refreshManager;
        _tokenStore = tokenStore;
        _navManager = navManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await _tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var newAccessToken = await _refreshManager.TryRefreshTokenAsync();

            if (!string.IsNullOrEmpty(newAccessToken))
            {
                var newRequest = await CloneHttpRequestMessageAsync(request);

                // Neuen Header setzen
                newRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", newAccessToken);

                // Request wiederholen
                response = await base.SendAsync(newRequest, cancellationToken);
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