using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Core.Interfaces;

public interface IExternalAuthService
{
    AuthenticationProperties GetAuthProperties(string provider, string redirectUrl);
    string GetRedirectUrl(string provider, string? returnUrl);
    Task<IActionResult> HandleExternalCallbackAsync(string? returnUrl, string? remoteError);
}