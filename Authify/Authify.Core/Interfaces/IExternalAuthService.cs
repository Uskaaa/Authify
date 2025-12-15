using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Core.Interfaces;

public interface IExternalAuthService
{
    AuthenticationProperties GetAuthProperties(string provider, string redirectUrl, string userId);
    string GetRedirectUrl(string provider, string? returnUrl, string mode);
    Task<IActionResult> HandleExternalCallbackAsync(string? returnUrl, string mode, string? remoteError);
}