using System.Text.Json;
using Authify.Application.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public interface IUserDataExportService<TUser>
    where TUser : ApplicationUser
{
    Task<byte[]> ExportUserDataAsync(TUser user);
}

public class UserDataExportService<TUser> : IUserDataExportService<TUser>
    where TUser : ApplicationUser
{
    private readonly IAuthifyDbContext _context;

    public UserDataExportService(IAuthifyDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportUserDataAsync(TUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        // 1. IdentityUser Daten
        var identityUserData = new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.PhoneNumberConfirmed,
            user.LockoutEnabled,
            user.LockoutEnd
        };

        // 2. UserTwoFactors
        var twoFactors = await _context.UserTwoFactors
            .Where(x => x.UserId == user.Id)
            .Select(x => new
            {
                x.Method,
                x.IsEnabled,
                x.Priority
            })
            .ToListAsync();

        // 3. UserProfiles
        var profiles = await _context.UserProfiles
            .Where(x => x.UserId == user.Id)
            .Select(x => new
            {
                x.FullName,
                x.JobTitle,
                x.Company,
                x.Bio,
                x.ProfileImage
            })
            .ToListAsync();

        // Zusammenführen
        var exportData = new
        {
            IdentityUser = identityUserData,
            TwoFactors = twoFactors,
            Profiles = profiles
        };

        // Als JSON serialisieren
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return jsonBytes;
    }
}