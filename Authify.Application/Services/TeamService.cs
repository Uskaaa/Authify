using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Extensions;
using Authify.Core.Interfaces;
using Authify.Core.Models.Teams;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class TeamService<TUser> : ITeamService
    where TUser : ApplicationUser, new()
{
    private readonly ITeamDbContext _db;
    private readonly UserManager<TUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly InfrastructureOptions _options;

    public TeamService(ITeamDbContext db, UserManager<TUser> userManager,
        IEmailSender emailSender, InfrastructureOptions options)
    {
        _db = db;
        _userManager = userManager;
        _emailSender = emailSender;
        _options = options;
    }

    public async Task<OperationResult<TeamDto>> CreateTeamAsync(string adminUserId, CreateTeamRequest request)
    {
        var existing = await _db.Teams.FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);
        if (existing != null)
            return OperationResult<TeamDto>.Fail("Du hast bereits ein Team erstellt.");

        var alreadyMember = await _db.TeamMembers.AnyAsync(m => m.UserId == adminUserId);
        if (alreadyMember)
            return OperationResult<TeamDto>.Fail("Du bist bereits Mitglied eines Teams und kannst kein neues Team erstellen.");

        var team = new Team
        {
            Name = request.Name,
            Description = request.Description,
            CompanyAddress = request.CompanyAddress,
            Website = request.Website,
            AdminUserId = adminUserId
        };

        _db.Teams.Add(team);

        var adminMember = new TeamMember
        {
            TeamId = team.Id,
            UserId = adminUserId,
            Role = TeamMemberRole.Admin
        };
        _db.TeamMembers.Add(adminMember);

        await _db.SaveChangesAsync();

        return OperationResult<TeamDto>.Ok(MapToDto(team, 1));
    }

    public async Task<OperationResult<TeamDto>> GetTeamByAdminAsync(string adminUserId)
    {
        var team = await _db.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);

        if (team == null)
            return OperationResult<TeamDto>.Fail("Kein Team gefunden.");

        return OperationResult<TeamDto>.Ok(MapToDto(team, team.Members.Count));
    }

    public async Task<OperationResult<TeamDto>> GetTeamByMemberAsync(string userId)
    {
        var member = await _db.TeamMembers
            .Include(m => m.Team)
            .ThenInclude(t => t!.Members)
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (member?.Team == null)
            return OperationResult<TeamDto>.Fail("Kein Team gefunden.");

        return OperationResult<TeamDto>.Ok(MapToDto(member.Team, member.Team.Members.Count));
    }

    public async Task<OperationResult<TeamDto>> UpdateTeamAsync(string adminUserId, UpdateTeamRequest request)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);
        if (team == null)
            return OperationResult<TeamDto>.Fail("Team nicht gefunden.");

        team.Name = request.Name;
        team.Description = request.Description;
        team.CompanyAddress = request.CompanyAddress;
        team.Website = request.Website;

        await _db.SaveChangesAsync();

        var memberCount = await _db.TeamMembers.CountAsync(m => m.TeamId == team.Id);
        return OperationResult<TeamDto>.Ok(MapToDto(team, memberCount));
    }

    public async Task<OperationResult> DeleteTeamAsync(string adminUserId)
    {
        var team = await _db.Teams
            .Include(t => t.Members)
            .Include(t => t.Invitations)
            .FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);

        if (team == null)
            return OperationResult.Fail("Team nicht gefunden.");

        _db.TeamMembers.RemoveRange(team.Members);
        _db.TeamInvitations.RemoveRange(team.Invitations);
        _db.Teams.Remove(team);

        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult<List<TeamMemberDto>>> GetMembersAsync(string adminUserId)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);
        if (team == null)
            return OperationResult<List<TeamMemberDto>>.Fail("Team nicht gefunden.");

        var members = await _db.TeamMembers
            .Where(m => m.TeamId == team.Id)
            .ToListAsync();

        var dtos = new List<TeamMemberDto>();
        foreach (var member in members)
        {
            var user = await _userManager.FindByIdAsync(member.UserId);
            if (user == null) continue;

            dtos.Add(new TeamMemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                FullName = user.FullName ?? user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = member.Role,
                JoinedAt = member.JoinedAt
            });
        }

        return OperationResult<List<TeamMemberDto>>.Ok(dtos);
    }

    public async Task<OperationResult<TeamMemberDto>> CreateMemberAsync(string adminUserId, CreateTeamMemberRequest request)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);
        if (team == null)
            return OperationResult<TeamMemberDto>.Fail("Team nicht gefunden.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            var alreadyMember = await _db.TeamMembers.AnyAsync(m => m.UserId == existingUser.Id && m.TeamId == team.Id);
            if (alreadyMember)
                return OperationResult<TeamMemberDto>.Fail("Dieser Nutzer ist bereits Mitglied des Teams.");
        }

        var tempPassword = request.TemporaryPassword ?? GenerateTemporaryPassword();
        bool isNewUser = existingUser == null;

        TUser? user;
        if (isNewUser)
        {
            user = new TUser
            {
                FullName = request.FullName,
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, tempPassword);
            if (!createResult.Succeeded)
                return OperationResult<TeamMemberDto>.Fail(string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
        else
        {
            user = existingUser;
        }

        var member = new TeamMember
        {
            TeamId = team.Id,
            UserId = user.Id,
            Role = TeamMemberRole.Member
        };

        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync();

        // Passwort-Reset-Link generieren und per E-Mail senden
        string? returnedPassword = null;
        if (isNewUser)
        {
            try
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = Uri.EscapeDataString(resetToken);
                var encodedEmail = Uri.EscapeDataString(user.Email!);
                var resetLink = $"{_options.Domain.TrimEnd('/')}/reset-password?email={encodedEmail}&token={encodedToken}&invited=true";

                var html = MycelisEmailTemplate.BuildActionEmail(
                    title: "Du wurdest zum Team hinzugefügt",
                    intro: $"Hallo {user.FullName}, du wurdest dem Team {team.Name} hinzugefügt.",
                    actionLabel: "Passwort festlegen",
                    actionUrl: resetLink,
                    outro: "Der Link ist 24 Stunden gültig.");

                await _emailSender.SendEmailAsync(user.Email!, $"Du wurdest zu {team.Name} hinzugefügt", html);
            }
            catch
            {
                // E-Mail-Versand fehlgeschlagen – Admin erhält das Passwort zur manuellen Weitergabe
                returnedPassword = tempPassword;
            }
        }

        return OperationResult<TeamMemberDto>.Ok(new TeamMemberDto
        {
            Id = member.Id,
            UserId = user.Id,
            FullName = user.FullName ?? request.FullName,
            Email = user.Email ?? request.Email,
            Role = member.Role,
            JoinedAt = member.JoinedAt,
            TemporaryPassword = returnedPassword
        });
    }

    public async Task<OperationResult> RemoveMemberAsync(string adminUserId, string memberId)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);
        if (team == null)
            return OperationResult.Fail("Team nicht gefunden.");

        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.TeamId == team.Id);
        if (member == null)
            return OperationResult.Fail("Mitglied nicht gefunden.");

        if (member.UserId == adminUserId)
            return OperationResult.Fail("Der Admin-Account kann nicht entfernt werden.");

        _db.TeamMembers.Remove(member);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult<bool>> IsTeamAdminAsync(string userId)
    {
        var isAdmin = await _db.Teams.AnyAsync(t => t.AdminUserId == userId);
        return OperationResult<bool>.Ok(isAdmin);
    }

    public async Task<OperationResult<bool>> IsTeamMemberAsync(string userId)
    {
        var isMember = await _db.TeamMembers.AnyAsync(m => m.UserId == userId);
        return OperationResult<bool>.Ok(isMember);
    }

    private static TeamDto MapToDto(Team team, int memberCount) => new()
    {
        Id = team.Id,
        Name = team.Name,
        Description = team.Description,
        CompanyAddress = team.CompanyAddress,
        Website = team.Website,
        AdminUserId = team.AdminUserId,
        CreatedAt = team.CreatedAt,
        MemberCount = memberCount
    };

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray()) + "A1!";
    }
}
