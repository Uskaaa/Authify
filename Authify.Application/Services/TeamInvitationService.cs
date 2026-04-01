using System.Security.Cryptography;
using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Extensions;
using Authify.Core.Interfaces;
using Authify.Core.Models.Teams;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Services;

public class TeamInvitationService<TUser> : ITeamInvitationService
    where TUser : ApplicationUser, new()
{
    private readonly ITeamDbContext _db;
    private readonly UserManager<TUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly InfrastructureOptions _options;

    public TeamInvitationService(ITeamDbContext db, UserManager<TUser> userManager,
        IEmailSender emailSender, InfrastructureOptions options)
    {
        _db = db;
        _userManager = userManager;
        _emailSender = emailSender;
        _options = options;
    }

    public async Task<OperationResult<TeamInvitationDto>> CreateInvitationAsync(string adminUserId, CreateInvitationRequest request)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);
        if (team == null)
            return OperationResult<TeamInvitationDto>.Fail("Team nicht gefunden.");

        // Bei E-Mail-spezifischer Einladung: Nur einmal gültig
        if (!string.IsNullOrWhiteSpace(request.Email))
            request.MaxUses = 1;

        var token = GenerateSecureToken();

        var invitation = new TeamInvitation
        {
            TeamId = team.Id,
            Token = token,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
            MaxUses = request.MaxUses,
            ExpiresAt = DateTime.UtcNow.AddDays(Math.Clamp(request.ExpirationDays, 1, 30)),
            CreatedByUserId = adminUserId
        };

        _db.TeamInvitations.Add(invitation);
        await _db.SaveChangesAsync();

        // Bei persönlicher Einladung: E-Mail versenden
        if (!string.IsNullOrEmpty(invitation.Email))
        {
            var inviteLink = $"{_options.Domain.TrimEnd("/")}/accept-invitation?token={Uri.EscapeDataString(token)}";
            await SendInvitationEmailAsync(invitation.Email, team.Name, inviteLink);
        }

        return OperationResult<TeamInvitationDto>.Ok(MapToDto(invitation, team.Name));
    }

    public async Task<OperationResult<List<TeamInvitationDto>>> GetInvitationsAsync(string adminUserId)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);
        if (team == null)
            return OperationResult<List<TeamInvitationDto>>.Fail("Team nicht gefunden.");

        var invitations = await _db.TeamInvitations
            .Where(i => i.TeamId == team.Id)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return OperationResult<List<TeamInvitationDto>>.Ok(
            invitations.Select(i => MapToDto(i, team.Name)).ToList());
    }

    public async Task<OperationResult> RevokeInvitationAsync(string adminUserId, string invitationId)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.AdminUserId == adminUserId);
        if (team == null)
            return OperationResult.Fail("Team nicht gefunden.");

        var invitation = await _db.TeamInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.TeamId == team.Id);

        if (invitation == null)
            return OperationResult.Fail("Einladung nicht gefunden.");

        invitation.IsRevoked = true;
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult<TeamInvitationDto>> GetInvitationByTokenAsync(string token)
    {
        var invitation = await _db.TeamInvitations
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation?.Team == null)
            return OperationResult<TeamInvitationDto>.Fail("Einladung nicht gefunden.");

        if (invitation.IsRevoked)
            return OperationResult<TeamInvitationDto>.Fail("Diese Einladung wurde widerrufen.");

        if (DateTime.UtcNow > invitation.ExpiresAt)
            return OperationResult<TeamInvitationDto>.Fail("Diese Einladung ist abgelaufen.");

        if (invitation.MaxUses.HasValue && invitation.UsedCount >= invitation.MaxUses.Value)
            return OperationResult<TeamInvitationDto>.Fail("Das Nutzungslimit dieser Einladung wurde erreicht.");

        return OperationResult<TeamInvitationDto>.Ok(MapToDto(invitation, invitation.Team.Name));
    }

    public async Task<OperationResult<string>> AcceptInvitationAsync(AcceptInvitationRequest request)
    {
        var invitation = await _db.TeamInvitations
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Token == request.Token);

        if (invitation?.Team == null)
            return OperationResult<string>.Fail("Einladung nicht gefunden.");

        if (invitation.IsRevoked)
            return OperationResult<string>.Fail("Diese Einladung wurde widerrufen.");

        if (DateTime.UtcNow > invitation.ExpiresAt)
            return OperationResult<string>.Fail("Diese Einladung ist abgelaufen.");

        if (invitation.MaxUses.HasValue && invitation.UsedCount >= invitation.MaxUses.Value)
            return OperationResult<string>.Fail("Das Nutzungslimit dieser Einladung wurde erreicht.");

        // E-Mail-spezifische Einladung: Adresse muss übereinstimmen
        if (!string.IsNullOrEmpty(invitation.Email) &&
            !string.Equals(invitation.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return OperationResult<string>.Fail("Diese Einladung gilt nur für eine andere E-Mail-Adresse.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            var alreadyMember = await _db.TeamMembers.AnyAsync(m => m.UserId == existingUser.Id);
            if (alreadyMember)
                return OperationResult<string>.Fail("Du bist bereits Mitglied eines Teams.");
        }

        TUser user;
        if (existingUser == null)
        {
            user = new TUser
            {
                FullName = request.FullName,
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
                return OperationResult<string>.Fail(string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
        else
        {
            user = existingUser;
            var addPasswordResult = await _userManager.AddPasswordAsync(user, request.Password);
            if (!addPasswordResult.Succeeded)
            {
                // Nutzer hat schon ein Passwort – trotzdem dem Team hinzufügen
            }
        }

        var member = new TeamMember
        {
            TeamId = invitation.TeamId,
            UserId = user.Id,
            Role = TeamMemberRole.Member
        };

        _db.TeamMembers.Add(member);
        invitation.UsedCount++;
        await _db.SaveChangesAsync();

        // Passwort-Reset-Token zurückgeben, damit der Nutzer direkt zu change-password weitergeleitet wird
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(resetToken);
        var encodedEmail = Uri.EscapeDataString(user.Email!);

        return OperationResult<string>.Ok($"reset-password?email={encodedEmail}&token={encodedToken}&invited=true");
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static TeamInvitationDto MapToDto(TeamInvitation invitation, string teamName) => new()
    {
        Id = invitation.Id,
        TeamId = invitation.TeamId,
        TeamName = teamName,
        Token = invitation.Token,
        Email = invitation.Email,
        MaxUses = invitation.MaxUses,
        UsedCount = invitation.UsedCount,
        IsRevoked = invitation.IsRevoked,
        CreatedAt = invitation.CreatedAt,
        ExpiresAt = invitation.ExpiresAt
    };

    private async Task SendInvitationEmailAsync(string email, string teamName, string inviteLink)
    {
        var html = MycelisEmailTemplate.BuildActionEmail(
            title: "Team-Einladung",
            intro: $"Du wurdest eingeladen, dem Team {teamName} beizutreten.",
            actionLabel: "Einladung annehmen",
            actionUrl: inviteLink,
            outro: "Falls du diese Einladung nicht erwartet hast, kannst du diese E-Mail ignorieren.");

        await _emailSender.SendEmailAsync(email, $"Einladung zum Team: {teamName}", html);
    }
}
