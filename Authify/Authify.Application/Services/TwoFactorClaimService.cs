using System.Security.Claims;
using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class TwoFactorClaimService<TUser> : ITwoFactorClaimService
    where TUser : IdentityUser
{
    private readonly UserManager<TUser> _userManager;
    private readonly IAuthifyDbContext _context;

    public TwoFactorClaimService(UserManager<TUser> userManager, IAuthifyDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // Hinzufügen oder Aktualisieren einer Methode
    public async Task<OperationResult> AddOrUpdateAsync(string userId, TwoFactorRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var existing = await _context.UserTwoFactors
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Method == request.TwoFactorMethod);

        if (existing != null)
        {
            existing.IsEnabled = request.IsEnabled;
            existing.Priority = request.Priority;
        }
        else
        {
            await _context.UserTwoFactors.AddAsync(new UserTwoFactor
            {
                UserId = userId,
                Method = request.TwoFactorMethod,
                IsEnabled = request.IsEnabled,
                Priority = request.Priority
            });
        }

        await _context.SaveChangesAsync();
        return OperationResult.Ok();
    }

    // Entfernen einer Methode
    public async Task<OperationResult> RemoveAsync(string userId, TwoFactorRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var existing = await _context.UserTwoFactors
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Method == request.TwoFactorMethod);

        if (existing == null)
            return OperationResult.Fail("TwoFactor method not found.");

        _context.UserTwoFactors.Remove(existing);
        await _context.SaveChangesAsync();

        return OperationResult.Ok();
    }

    // Alle Methoden eines Users abfragen
    public async Task<OperationResult<List<UserTwoFactor>>> GetAllAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult<List<UserTwoFactor>>.Fail("User not found.");

        var methods = await _context.UserTwoFactors
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Priority)
            .ToListAsync();

        return methods.Count == 0
            ? OperationResult<List<UserTwoFactor>>.Fail("No two-factor methods found for the user.")
            : OperationResult<List<UserTwoFactor>>.Ok(methods);
    }

    // Bevorzugte Methode abfragen
    public async Task<OperationResult<UserTwoFactor>> GetPreferredAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult<UserTwoFactor>.Fail("User not found.");

        var preferred = await _context.UserTwoFactors
            .Where(x => x.UserId == userId && x.IsEnabled)
            .OrderBy(x => x.Priority)
            .FirstOrDefaultAsync();

        return preferred == null
            ? OperationResult<UserTwoFactor>.Fail("No enabled two-factor method found.")
            : OperationResult<UserTwoFactor>.Ok(preferred);
    }
}