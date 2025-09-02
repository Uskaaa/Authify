using Authify.Application.Data;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RefreshTokenController<TUser> : ControllerBase
    where TUser : IdentityUser
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthifyDbContext _context;
    private readonly UserManager<TUser> _userManager;

    public RefreshTokenController(IJwtTokenService jwtTokenService, IAuthifyDbContext context, UserManager<TUser> userManager)
    {
        _jwtTokenService = jwtTokenService;
        _context = context;
        _userManager = userManager;
    }

    [HttpPost("renew")]
    public async Task<IActionResult> Renew([FromBody] RefreshTokenRequest request)
    {
        // RefreshToken prüfen
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);

        if (storedToken == null)
            return Unauthorized(new { error = "Invalid or expired refresh token." });

        // UserId aus dem RefreshToken
        var user = await _userManager.FindByIdAsync(storedToken.UserId);

        // Neuen JWT generieren
        if (user != null)
        {
            var jwtToken = _jwtTokenService.GenerateToken(user);

            // Optional: neuen RefreshToken erstellen
            storedToken.IsRevoked = true; // alten token ungültig machen
            var newRefreshToken =
                _jwtTokenService.GenerateRefreshToken(storedToken.UserId, request.DeviceName, request.IpAddress, request.RememberMe);
            await _context.RefreshTokens.AddAsync(newRefreshToken);
            await _context.SaveChangesAsync();

            var refreshTokenRequest = new RefreshTokenRequest
            {
                RefreshToken = newRefreshToken.Token,
                DeviceName = newRefreshToken.DeviceInfo,
                IpAddress = newRefreshToken.IpAddress,
                RememberMe = newRefreshToken.RememberMe
            };
            
            return Ok(new
            {
                AccessToken = jwtToken,
                RefreshToken = refreshTokenRequest
            });
        }
        return Unauthorized(new { error = "No User was found!" });
    }
}