using System.Security.Claims;
using System.Security.Cryptography;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Authify.Api.Controllers;

[ApiController]
[Route("oidc")]
[IgnoreAntiforgeryToken]
public class OidcProviderController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IJwtTokenService _jwtService;
    private readonly IMemoryCache _cache;
    private readonly ITeamService _teamService;
    private readonly IPersonalAccessTokenService _patService;

    // mycelis_change - reserved client_id for the Mycelis CLI's browser-login handoff (see AuthorizeApi/Token below).
    // Unlike mycelis-client-{userId}/{teamId}, the CLI can't know this value in advance since it doesn't know
    // who's signing in yet, so it's a fixed, always-allowed client_id rather than one derived per-user.
    private const string CliClientId = "mycelis-cli";

    private static readonly RSA _rsa = RSA.Create(2048);
    private static readonly RsaSecurityKey _rsaKey = new RsaSecurityKey(_rsa) { KeyId = "privateai-key-1" };

    public OidcProviderController(
        IConfiguration config,
        IJwtTokenService jwtService,
        IMemoryCache cache,
        ITeamService teamService,
        IPersonalAccessTokenService patService)
    {
        _config = config;
        _jwtService = jwtService;
        _cache = cache;
        _teamService = teamService;
        _patService = patService;
    }

    [HttpGet(".well-known/openid-configuration")]
    public IActionResult GetConfiguration()
    {
        var browserDomain = _config["App:Domain"] ?? "http://localhost:5220";
        var dockerDomain = _config["ManagedUiHosting:InternalWebAppUrl"] ?? "http://host.docker.internal:5220";

        return Ok(new
        {
            issuer = _config["Jwt:Issuer"] ?? browserDomain,
            authorization_endpoint = $"{browserDomain.TrimEnd("/")}/oidc/authorize",
            token_endpoint = $"{dockerDomain}/oidc/token",
            userinfo_endpoint = $"{dockerDomain}/oidc/userinfo",
            jwks_uri = $"{dockerDomain}/oidc/jwks", 
            
            response_types_supported = new[] { "code" },
            id_token_signing_alg_values_supported = new[] { "RS256" }, 
            subject_types_supported = new[] { "public" }
        });
    }

    [HttpGet("jwks")]
    public IActionResult Jwks()
    {
        var publicParameters = _rsa.ExportParameters(false);
        var publicKey = new RsaSecurityKey(publicParameters) { KeyId = _rsaKey.KeyId };

        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
        jwk.Use = "sig";
        jwk.Alg = "RS256";
        
        jwk.KeyOps.Clear();
        jwk.KeyOps.Add("verify");

        return Ok(new { keys = new[] { jwk } });
    }

    [HttpGet("authorize")]
    public IActionResult Authorize([FromQuery] string? redirect_uri, [FromQuery] string? state, [FromQuery] string? client_id, [FromQuery] string? nonce)
    {
        var html = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1'>
            <title>Authenticating...</title>
            <script>
                // Theme: dark = default, light = [data-theme=""light""]
                function resolveTheme() {{
                    var saved = localStorage.getItem('color-theme');
                    if (saved) return saved;
                    return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
                }}

                window.applyTheme = function () {{
                    document.documentElement.dataset.theme = resolveTheme();
                }};

                window.toggleTheme = function () {{
                    var current = document.documentElement.dataset.theme;
                    var next = current === 'light' ? 'dark' : 'light';
                    document.documentElement.dataset.theme = next;
                    localStorage.setItem('color-theme', next);
                }};

                // Run once before CSS is parsed to avoid theme flash on first paint.
                window.applyTheme();

                // Re-apply after enhanced navigation updates page content without full reload.
                document.addEventListener('enhancedload', window.applyTheme);
            </script>
            <style>
                :root {{
                    --bg: #101214;
                    --bg-surface: #171a1f;
                    --text-primary: #f5f7fb;
                    --text-secondary: #c2c9d4;
                    --text-muted: #9aa3b2;
                    --border: rgba(255, 255, 255, 0.16);
                }}
                [data-theme='light'] {{
                    --bg: #F5F5F2;
                    --bg-surface: #FFFFFF;
                    --text-primary: #0A0A0A;
                    --text-secondary: #666666;
                    --text-muted: #AAAAAA;
                    --border: rgba(0, 0, 0, 0.08);
                }}
                body {{
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    min-height: 100vh;
                    margin: 0;
                    padding: 24px;
                    background: var(--bg);
                    color: var(--text-primary);
                    font-family: 'DM Sans', ui-sans-serif, system-ui, sans-serif;
                }}
                .container {{
                    text-align: center;
                    background: var(--bg-surface);
                    border: 1px solid var(--border);
                    border-radius: 8px;
                    padding: 2rem 2.5rem;
                    min-width: min(100%, 360px);
                    box-shadow: 0 8px 40px rgba(0, 0, 0, 0.2);
                }}
                .brand {{
                    margin: 0 0 10px;
                    font-size: 12px;
                    letter-spacing: 0.08em;
                    text-transform: uppercase;
                    color: var(--text-muted);
                }}
                h3 {{
                    margin: 0;
                    font-family: 'Syne', 'DM Sans', sans-serif;
                    font-size: 24px;
                    font-weight: 600;
                    color: var(--text-primary);
                }}
                p {{
                    margin: 8px 0 0;
                    font-size: 14px;
                    color: var(--text-secondary);
                }}
                .loader {{
                    border: 3px solid var(--border);
                    border-top-color: var(--text-primary);
                    border-radius: 50%;
                    width: 32px;
                    height: 32px;
                    animation: spin 1s linear infinite;
                    margin: 0 auto 16px;
                }}
                @keyframes spin {{ 0% {{ transform: rotate(0deg); }} 100% {{ transform: rotate(360deg); }} }}
            </style>
        </head>
        <body>
            <div class='container'>
                <p class='brand'>Mycelis</p>
                <div class='loader'></div>
                <h3>Authenticating...</h3>
                <p>Securing your session.</p>
            </div>

            <script>
                function redirectToLogin() {{
                    const currentUrl = encodeURIComponent(window.location.href);
                    window.location.replace('/login?returnUrl=' + currentUrl);
                }}

                // mycelis_change - a rejected client_id (403) or any other server error used to be
                // swallowed and treated the same as not-logged-in-yet, bouncing back to /login. If
                // the user was already logged in, that bounce lands right back here and repeats
                // forever, which just looks like a stuck/blank page with no indication of what is wrong.
                // Only a 401 (missing/expired session) is actually fixed by logging in again.
                function showError(message) {{
                    var container = document.querySelector('.container');
                    if (!container) return;
                    container.innerHTML = '';
                    var brand = document.createElement('p');
                    brand.className = 'brand';
                    brand.textContent = 'Mycelis';
                    var h3 = document.createElement('h3');
                    h3.textContent = 'Sign-in failed';
                    var p = document.createElement('p');
                    p.textContent = message;
                    container.appendChild(brand);
                    container.appendChild(h3);
                    container.appendChild(p);
                }}

                let token = localStorage.getItem('auth_access_token');

                if (!token) {{
                    redirectToLogin();
                }} else {{
                    token = token.replace(/^""|""$/g, '');

                    const apiUrl = `/oidc/authorize-api?redirect_uri=${{encodeURIComponent('{redirect_uri ?? ""}')}}&state=${{encodeURIComponent('{state ?? ""}')}}&client_id=${{encodeURIComponent('{client_id ?? ""}')}}&nonce=${{encodeURIComponent('{nonce ?? ""}')}}`;

                    fetch(apiUrl, {{
                        method: 'GET',
                        headers: {{ 'Authorization': 'Bearer ' + token }}
                    }})
                    .then(response => {{
                        if (response.status === 401) {{
                            redirectToLogin();
                            return null;
                        }}
                        if (!response.ok) {{
                            return response.text().then(text => {{
                                throw new Error(text || ('Request failed (' + response.status + ')'));
                            }});
                        }}
                        return response.json();
                    }})
                    .then(data => {{
                        if (!data) return;
                        window.location.replace(data.redirectUrl);
                    }})
                    .catch(error => {{
                        showError(error && error.message ? error.message : 'Sign-in failed.');
                    }});
                }}
            </script>
        </body>
        </html>";

        return Content(html, "text/html");
    }

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet("authorize-api")] 
    public async Task<IActionResult> AuthorizeApi(
        [FromQuery] string? redirect_uri, 
        [FromQuery] string? state, 
        [FromQuery] string? nonce,
        [FromQuery] string? client_id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(redirect_uri)) 
            return BadRequest("Fehlende Parameter.");

        // Erwartete Client-IDs für diesen Nutzer:
        var personalClientId = $"mycelis-client-{userId}";

        // Prüfen ob Nutzer in einem Team ist
        var teamResult = await _teamService.GetTeamByMemberAsync(userId);
        var teamClientId = teamResult.Success ? $"mycelis-client-{teamResult.Data!.Id}" : null;

        // mycelis_change - the CLI's fixed client_id is valid for any authenticated user, not tied to a specific instance.
        if (client_id != personalClientId && (teamClientId == null || client_id != teamClientId) && client_id != CliClientId)
        {
            return StatusCode(403, "Zugriff verweigert! Du bist nicht der Besitzer oder Mitglied des Teams dieser Instanz.");
        }
        
        var code = Guid.NewGuid().ToString("N");
        
        var sessionData = new OidcSessionData { UserId = userId, Nonce = nonce, ClientId = client_id };
        _cache.Set(code, sessionData, TimeSpan.FromMinutes(5));

        var targetUrl = $"{redirect_uri}?code={code}&state={state}";
        
        return Ok(new { redirectUrl = targetUrl });
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token([FromForm] string code)
    {
        if (!_cache.TryGetValue(code, out OidcSessionData? sessionData) || sessionData == null)
            return BadRequest(new { error = "invalid_grant" });
        
        _cache.Remove(code); 

        var userId = sessionData.UserId;
        var nonce = sessionData.Nonce;
        var clientId = sessionData.ClientId ?? "mycelis-client";

        // 1. Access Token
        var accessToken = await _jwtService.GenerateTokenAsync(userId);
        
        // 2. ID Token (RS256 Token für OpenWebUI)
        var domain = _config["App:Domain"] ?? "http://localhost:5220";
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim("azp", clientId)
        };
        
        if (!string.IsNullOrEmpty(nonce))
        {
            claims.Add(new Claim("nonce", nonce));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _config["Jwt:Issuer"] ?? domain,
            Audience = clientId, 
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(_rsaKey, SecurityAlgorithms.RsaSha256) 
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var idToken = tokenHandler.CreateToken(tokenDescriptor);

        // mycelis_change start - the CLI can't call the model gateway with this short-lived access_token
        // (AuthifyPatResolver only resolves real PATs by hash), so mint one alongside it here, using the
        // same TenantId-resolution rule as ApiKeysController.Create.
        string? cliPat = null;
        if (clientId == CliClientId)
        {
            var cliTeamResult = await _teamService.GetTeamByMemberAsync(userId);
            var tenantId = (cliTeamResult.Success && cliTeamResult.Data != null) ? cliTeamResult.Data.Id : userId;

            var patResult = await _patService.CreateAsync(userId, new CreatePersonalAccessTokenRequest
            {
                Name = "Mycelis CLI",
                TenantId = tenantId,
                EndUserId = userId,
                Scopes = ["cli"]
            });

            if (patResult.Success)
                cliPat = patResult.Data!.Token;
        }
        // mycelis_change end

        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            id_token = tokenHandler.WriteToken(idToken),
            pat = cliPat // mycelis_change
        });
    }

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet("userinfo")]
    public IActionResult UserInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        var name = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? email;

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        return Ok(new
        {
            sub = userId,
            name = name ?? "Mycelis User",
            email = email ?? $"{userId}@mycelis.local",
            email_verified = true
        });
    }

    // Hilfsklasse for Cache
    private class OidcSessionData
    {
        public required string UserId { get; set; }
        public string? Nonce { get; set; }
        public string? ClientId { get; set; }
    }
}
