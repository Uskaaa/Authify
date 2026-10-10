using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class UserAccountService<TUser> : IUserAccountService
        where TUser : ApplicationUser
    {
        private readonly UserManager<TUser> _userManager;
        private readonly IAuthifyDbContext _context;
        private readonly IUserDataExportService<TUser> _userDataExportService;
        private readonly ITeamService? _teamService;

        public UserAccountService(UserManager<TUser> userManager, IAuthifyDbContext context, IUserDataExportService<TUser> userDataExportService,
            IEnumerable<ITeamService> teamServices)
        {
            _userManager = userManager;
            _context = context;
            _userDataExportService = userDataExportService;
            _teamService = teamServices.FirstOrDefault(); // only registered when the team feature is on
        }

        // ---- Export ----
        public async Task<OperationResult<byte[]>> RequestExportAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return OperationResult<byte[]>.Fail("User not found.");

            // ExportDateiname generieren
            var fileName = $"user_export_{user.UserName}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

            var exportRequest = new UserExportRequest
            {
                UserId = userId,
                ExportFileName = fileName
            };

            await _context.UserExportRequests.AddAsync(exportRequest);
            await _context.SaveChangesAsync();

            // Benutzer-Daten exportieren
            var exportBytes = await _userDataExportService.ExportUserDataAsync(user);

            // Optional: exportRequest.ExportDataJson = Encoding.UTF8.GetString(exportBytes);
            // await _context.SaveChangesAsync();

            return OperationResult<byte[]>.Ok(exportBytes);
        }


        public async Task<OperationResult<UserExportRequest>> GetExportStatusAsync(string userId)
        {
            var export = await _context.UserExportRequests
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.RequestedAt)
                .FirstOrDefaultAsync();

            return export == null
                ? OperationResult<UserExportRequest>.Fail("No export request found.")
                : OperationResult<UserExportRequest>.Ok(export);
        }

        // ---- Deactivate ----
        // A team account can't deactivate/delete itself: a member is managed by the team, and an admin would
        // leave the team (and everything it owns) orphaned – the admin deletes the team first.
        private async Task<bool> IsInTeamAsync(string userId)
        {
            if (_teamService == null) return false;
            var team = await _teamService.GetTeamByMemberAsync(userId);
            return team.Success && team.Data != null;
        }

        private const string TeamAccountMessage =
            "Dein Konto gehört zu einem Team und kann nicht selbst deaktiviert oder gelöscht werden.";

        public async Task<OperationResult> DeactivateAccountAsync(string userId)
        {
            if (await IsInTeamAsync(userId)) return OperationResult.Fail(TeamAccountMessage);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return OperationResult.Fail("User not found.");

            var deactivation = new UserDeactivationRequest { UserId = userId, IsDeactivated = true, DeactivatedAt = DateTime.UtcNow };
            await _context.UserDeactivationRequests.AddAsync(deactivation);
            await _context.SaveChangesAsync();

            // Optional: SignOut user, block login, etc.

            return OperationResult.Ok();
        }

        public async Task<OperationResult<UserDeactivationRequest>> GetDeactivationStatusAsync(string userId)
        {
            var deactivation = await _context.UserDeactivationRequests
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.RequestedAt)
                .FirstOrDefaultAsync();

            return deactivation == null
                ? OperationResult<UserDeactivationRequest>.Fail("No deactivation request found.")
                : OperationResult<UserDeactivationRequest>.Ok(deactivation);
        }

        // ---- Delete ----
        public async Task<OperationResult> DeleteAccountAsync(string userId)
        {
            if (await IsInTeamAsync(userId)) return OperationResult.Fail(TeamAccountMessage);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return OperationResult.Fail("User not found.");

            var deletion = new UserDeletionRequest { UserId = userId, IsDeleted = true, DeletedAt = DateTime.UtcNow };
            await _context.UserDeletionRequests.AddAsync(deletion);
            await _context.SaveChangesAsync();

            // Optional: delete user completely
            await _userManager.DeleteAsync(user);

            return OperationResult.Ok();
        }

        public async Task<OperationResult<UserDeletionRequest>> GetDeletionStatusAsync(string userId)
        {
            var deletion = await _context.UserDeletionRequests
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.RequestedAt)
                .FirstOrDefaultAsync();

            return deletion == null
                ? OperationResult<UserDeletionRequest>.Fail("No deletion request found.")
                : OperationResult<UserDeletionRequest>.Ok(deletion);
        }
    }