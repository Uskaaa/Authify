using Authify.Application.Data;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RefreshTokenController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthifyDbContext _context;

    public RefreshTokenController(IJwtTokenService jwtTokenService, IAuthifyDbContext context)
    {
        _jwtTokenService = jwtTokenService;
        _context = context;
    }

    [HttpPost("renew")]
    public async Task<IActionResult> Renew([FromBody] RefreshTokenRequest request)
    {
        // RefreshToken prüfen
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);

        if (storedToken == null)
            return Unauthorized(new { error = "Invalid or expired refresh token." });

        // Neuen JWT generieren
        var jwtToken = await _jwtTokenService.GenerateTokenAsync(storedToken.UserId);

        bool isNewDevice = storedToken.DeviceInfo != request.DeviceName;

        if (!isNewDevice)
        {
            storedToken.IsRevoked = true; 
        }

        var newRefreshToken =
            _jwtTokenService.GenerateRefreshToken(storedToken.UserId, request.DeviceName, request.IpAddress,
                storedToken.RememberMe);
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
            RefreshToken = newRefreshToken.Token
        });
    }
}