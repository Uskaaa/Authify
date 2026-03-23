using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Authify.Application.Data;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Authify.Application.Services;

public class JwtTokenService<TUser> : IJwtTokenService where TUser : ApplicationUser
{
    private readonly IConfiguration _config;
    private readonly UserManager<TUser> _userManager;
    private readonly ITeamService? _teamService;

    public JwtTokenService(IConfiguration config, UserManager<TUser> userManager, IServiceProvider serviceProvider)
    {
        _config = config;
        _userManager = userManager;
        _teamService = serviceProvider.GetService(typeof(ITeamService)) as ITeamService;
    }

    public async Task<string> GenerateTokenAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        
        // 1. Standard Claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName ?? "") 
        };

        // 1.1 Team Claims
        if (_teamService != null)
        {
            var isMemberResult = await _teamService.IsTeamMemberAsync(userId);
            if (isMemberResult.Success && isMemberResult.Data)
            {
                claims.Add(new Claim("is_team_member", "true"));
                
                var isAdminResult = await _teamService.IsTeamAdminAsync(userId);
                if (isAdminResult.Success && isAdminResult.Data)
                {
                    claims.Add(new Claim("is_team_admin", "true"));
                }
            }
        }

        // 2. Claims aus der Datenbank laden (z.B. user-spezifische Claims)
        var userClaims = await _userManager.GetClaimsAsync(user);
        if (userClaims.Count != 0)
            claims.AddRange(userClaims);

        // 3. Rollen laden und als Claims hinzufügen
        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Count != 0) claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(string userId, string deviceName, string ipAddress, bool rememberMe)
    {
        var expiry = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7);

        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = expiry,
            DeviceInfo = deviceName,
            IpAddress = ipAddress,
            RememberMe = rememberMe
        };
    }
}