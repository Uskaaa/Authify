using System.Security.Claims;
using System.Security.Cryptography;
using Authify.Core.Interfaces;
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


    private static readonly RSA _rsa = RSA.Create(2048);
    private static readonly RsaSecurityKey _rsaKey = new RsaSecurityKey(_rsa) { KeyId = "privateai-key-1" };

    public OidcProviderController(
        IConfiguration config, 
        IJwtTokenService jwtService,
        IMemoryCache cache,
        ITeamService teamService)
    {
        _config = config;
        _jwtService = jwtService;
        _cache = cache;
        _teamService = teamService;
    }

    [HttpGet(".well-known/openid-configuration")]
    public IActionResult GetConfiguration()
    {
        var browserDomain = _config["App:Domain"] ?? "http://localhost:5220";
        var dockerDomain = "http://host.docker.internal:5220";

        return Ok(new
        {
            issuer = _config["Jwt:Issuer"] ?? browserDomain,
            authorization_endpoint = $"{browserDomain}/oidc/authorize",
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
            <style>
                body {{ display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background-color: #f8fafc; font-family: ui-sans-serif, system-ui, sans-serif; color: #334155; }}
                .loader {{ border: 3px solid #e2e8f0; border-top-color: #4f46e5; border-radius: 50%; width: 32px; height: 32px; animation: spin 1s linear infinite; margin: 0 auto 16px; }}
                @keyframes spin {{ 0% {{ transform: rotate(0deg); }} 100% {{ transform: rotate(360deg); }} }}
                .container {{ text-align: center; background: white; padding: 2rem 3rem; border-radius: 12px; box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1); }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='loader'></div>
                <h3 style='margin:0; font-weight:600;'>Authenticating...</h3>
                <p style='margin:8px 0 0; font-size:14px; color:#64748b;'>Securing your session.</p>
            </div>

            <script>
                function redirectToLogin() {{
                    const currentUrl = encodeURIComponent(window.location.href);
                    window.location.replace('/login?returnUrl=' + currentUrl);
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
                        if (!response.ok) throw new Error('Unauthorized');
                        return response.json();
                    }})
                    .then(data => {{
                        window.location.replace(data.redirectUrl);
                    }})
                    .catch(() => {{
                        // Token abgelaufen oder ungültig -> Neu einloggen
                        redirectToLogin();
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
        var personalClientId = $"privateai-client-{userId}";
        
        // Prüfen ob Nutzer in einem Team ist
        var teamResult = await _teamService.GetTeamByMemberAsync(userId);
        var teamClientId = teamResult.Success ? $"privateai-client-{teamResult.Data!.Id}" : null;

        if (client_id != personalClientId && (teamClientId == null || client_id != teamClientId))
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
        var clientId = sessionData.ClientId ?? "privateai-client";

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
        
        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            id_token = tokenHandler.WriteToken(idToken)
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
            name = name ?? "PrivateAI User",
            email = email ?? $"{userId}@privateai.local",
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
