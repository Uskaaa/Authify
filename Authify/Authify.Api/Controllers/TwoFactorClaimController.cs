using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[Controller]")]
public class TwoFactorClaimController : Controller
{
    private readonly ITwoFactorClaimService _twoFactorClaimService;
    
    public TwoFactorClaimController(ITwoFactorClaimService twoFactorClaimService)
    {
        _twoFactorClaimService = twoFactorClaimService;
    }
    
    [HttpPost(nameof(AddClaim))]
    public async Task<IActionResult> AddClaim([FromBody] TwoFactorRequest request)
    {
        var result = await _twoFactorClaimService.AddClaimAsync(request);

        if (result.Success) return Ok();
        
        return BadRequest("Something went wrong.");
    }
    
    [HttpPost(nameof(CheckClaims))]
    public async Task<IActionResult> CheckClaims([FromBody] string userId)
    {
        var claims = await _twoFactorClaimService.CheckClaimsAsync(userId);

        if (claims.Success) return Ok(claims);
        
        return BadRequest("Something went wrong.");
    }
    
    [HttpPost(nameof(RemoveClaim))]
    public async Task<IActionResult> RemoveClaim([FromBody] TwoFactorRequest request)
    {
        var result = await _twoFactorClaimService.RemoveClaimAsync(request);

        if (result.Success) return Ok();
        
        return BadRequest("Something went wrong.");
    }
}