using System.Security.Claims;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Authify.Core.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class TwoFactorClaimService : ITwoFactorClaimService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly DbContext _dbContext;

    public TwoFactorClaimService(UserManager<IdentityUser> userManager, DbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    // Hinzufügen oder Aktualisieren einer Methode
    public async Task<OperationResult> AddOrUpdateAsync(TwoFactorRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var existing = await _dbContext.Set<UserTwoFactor>()
            .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.Method == request.TwoFactorMethod);

        if (existing != null)
        {
            existing.IsEnabled = request.IsEnabled;
            existing.Priority = request.Priority;
        }
        else
        {
            await _dbContext.Set<UserTwoFactor>().AddAsync(new UserTwoFactor
            {
                UserId = request.UserId,
                Method = request.TwoFactorMethod,
                IsEnabled = request.IsEnabled,
                Priority = request.Priority
            });
        }

        await _dbContext.SaveChangesAsync();
        return OperationResult.Ok();
    }

    // Entfernen einer Methode
    public async Task<OperationResult> RemoveAsync(TwoFactorRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var existing = await _dbContext.Set<UserTwoFactor>()
            .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.Method == request.TwoFactorMethod);

        if (existing == null)
            return OperationResult.Fail("TwoFactor method not found.");

        _dbContext.Set<UserTwoFactor>().Remove(existing);
        await _dbContext.SaveChangesAsync();

        return OperationResult.Ok();
    }

    // Alle Methoden eines Users abfragen
    public async Task<OperationResult> GetAllAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var methods = await _dbContext.Set<UserTwoFactor>()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Priority)
            .ToListAsync();

        return methods.Count == 0
            ? OperationResult.Fail("No two-factor methods found for the user.")
            : OperationResult<List<UserTwoFactor>>.Ok(methods);
    }

    // Bevorzugte Methode abfragen
    public async Task<OperationResult> GetPreferredAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found.");

        var preferred = await _dbContext.Set<UserTwoFactor>()
            .Where(x => x.UserId == userId && x.IsEnabled)
            .OrderBy(x => x.Priority)
            .FirstOrDefaultAsync();

        return preferred == null
            ? OperationResult.Fail("No enabled two-factor method found.")
            : OperationResult<UserTwoFactor>.Ok(preferred);
    }
}