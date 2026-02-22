using System.Security.Claims;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Authify.Api.Controllers;

[ApiController]
[Route("oidc")]
public class OidcProviderController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IJwtTokenService _jwtService;

    // Simpler In-Memory Cache für Auth Codes (in Produktion Redis nutzen!)
    private static readonly Dictionary<string, string> _authCodes = new();

    public OidcProviderController(
        IConfiguration config,
        IJwtTokenService jwtService)
    {
        _config = config;
        _jwtService = jwtService;
    }

    // 1. Discovery
    [HttpGet(".well-known/openid-configuration")]
    public IActionResult GetConfiguration()
    {
        var domain = _config["App:Domain"];
        return Ok(new
        {
            issuer = _config["Jwt:Issuer"],
            authorization_endpoint = $"{domain}/oidc/authorize",
            token_endpoint = $"{domain}/oidc/token",
            userinfo_endpoint = $"{domain}/oidc/userinfo",
            response_types_supported = new[] { "code" },
            id_token_signing_alg_values_supported = new[] { "HS256" }
        });
    }

    // 2. Authorize (Die Bridge)
    // Dieser Endpunkt wird vom Browser aufgerufen. Er liefert HTML+JS zurück.
    [HttpGet("authorize")]
    public IActionResult Authorize([FromQuery] string redirect_uri, [FromQuery] string state, [FromQuery] string client_id)
    {
        // Wir liefern eine HTML Seite, die das JWT aus dem LocalStorage liest
        // und damit den POST Request an /authorize-api macht.
        var html = $@"
        <html>
        <head><title>Authorizing...</title></head>
        <body>
            <h3>Authenticating via SimpliAI...</h3>
            <script>
                const token = localStorage.getItem('auth_access_token'); 

                if (!token) {{
                    // Nicht eingeloggt -> Redirect zum Login
                    window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.href);
                }} else {{
                    // Token gefunden -> Code anfordern
                    fetch('/oidc/authorize-api?redirect_uri={Uri.EscapeDataString(redirect_uri)}&state={Uri.EscapeDataString(state)}&client_id={Uri.EscapeDataString(client_id)}', {{
                        method: 'POST',
                        headers: {{ 'Authorization': 'Bearer ' + token }}
                    }})
                    .then(response => {{
                        if (response.ok) return response.json();
                        throw new Error('Unauthorized');
                    }})
                    .then(data => {{
                        // Redirect zurück zu OpenWebUI mit dem Code
                        window.location.href = data.redirectUrl;
                    }})
                    .catch(err => {{
                        // Token wohl abgelaufen -> Login
                        window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.href);
                    }});
                }}
            </script>
        </body>
        </html>";

        return Content(html, "text/html");
    }

    // 2b. Authorize API (Intern)
    // Hier kommt das JS mit dem Bearer Token hin.
    [Authorize]
    [HttpPost("authorize-api")]
    public IActionResult AuthorizeApi([FromQuery] string redirect_uri, [FromQuery] string state)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Code generieren
        var code = Guid.NewGuid().ToString();
        _authCodes[code] = userId!;

        // URL bauen, wohin das JS redirecten soll
        var targetUrl = $"{redirect_uri}?code={code}&state={state}";
        
        return Ok(new { redirectUrl = targetUrl });
    }

    // 3. Token Endpoint
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromForm] string code)
    {
        if (!_authCodes.ContainsKey(code))
            return BadRequest(new { error = "invalid_grant" });

        var userId = _authCodes[code];
        _authCodes.Remove(code); // One-Time-Use

        // JWT für OpenWebUI generieren
        // Wir nutzen deinen existierenden Service!
        var accessToken = await _jwtService.GenerateTokenAsync(userId);
        
        // OpenWebUI braucht ein "id_token", das JWT-Format hat. 
        // Wir nehmen einfach dein AccessToken auch als ID Token, da es User-Claims enthält.
        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            id_token = accessToken 
        });
    }

    // 4. UserInfo
    [Authorize]
    [HttpGet("userinfo")]
    public async Task<IActionResult> UserInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        var name = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? email;

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        return Ok(new
        {
            sub = userId,
            name = name ?? "PrivateAI User",
            email = email ?? $"{userId}@privateai.local",
            email_verified = true
        });
    }
}