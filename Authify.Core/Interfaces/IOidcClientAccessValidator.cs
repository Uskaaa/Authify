namespace Authify.Core.Interfaces;

/// <summary>
/// Optional host hook for the built-in OIDC provider: decides whether a signed-in user may obtain an
/// authorization code for <paramref name="clientId"/> delivered to <paramref name="redirectUri"/>.
/// </summary>
public interface IOidcClientAccessValidator
{
    /// <returns>true = allow, false = deny, null = no opinion (the next validator / the default rule decides).</returns>
    Task<bool?> IsAllowedAsync(string userId, string clientId, Uri redirectUri, CancellationToken cancellationToken = default);
}
