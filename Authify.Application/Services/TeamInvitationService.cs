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
    private readonly IAuthifyDbContext _authDb;
    private readonly UserManager<TUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly InfrastructureOptions _options;
    private readonly IEnumerable<ITeamLifecycleHook> _teamLifecycleHooks;

    public TeamInvitationService(ITeamDbContext db, IAuthifyDbContext authDb, UserManager<TUser> userManager,
        IEmailSender emailSender, InfrastructureOptions options,
        IEnumerable<ITeamLifecycleHook> teamLifecycleHooks)
    {
        _db = db;
        _authDb = authDb;
        _userManager = userManager;
        _emailSender = emailSender;
        _options = options;
        _teamLifecycleHooks = teamLifecycleHooks;
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
            ExpiresAt = request.ExpirationDays.HasValue
                ? DateTime.UtcNow.AddDays(Math.Clamp(request.ExpirationDays.Value, 1, 365))
                : null,
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

        if (invitation.ExpiresAt.HasValue && DateTime.UtcNow > invitation.ExpiresAt.Value)
            return OperationResult<TeamInvitationDto>.Fail("Diese Einladung ist abgelaufen.");

        if (invitation.MaxUses.HasValue && invitation.UsedCount >= invitation.MaxUses.Value)
            return OperationResult<TeamInvitationDto>.Fail("Das Nutzungslimit dieser Einladung wurde erreicht.");

        return OperationResult<TeamInvitationDto>.Ok(MapToDto(invitation, invitation.Team.Name));
    }

    /// <summary>
    /// Öffentlicher Flow: legt einen <b>neuen</b> Account an und tritt dem Team bei. Für eine E-Mail-Adresse,
    /// zu der bereits ein Konto existiert, wird abgelehnt – sonst könnte jeder mit einem Einladungslink
    /// (oder ein Admin mit einer selbst erstellten Einladung) einen fremden Account ins Team ziehen und
    /// über den zurückgegebenen Reset-Link übernehmen. Bestehende Nutzer nutzen <see cref="AcceptInvitationForUserAsync"/>.
    /// </summary>
    public async Task<OperationResult<string>> AcceptInvitationAsync(AcceptInvitationRequest request)
    {
        var invitationResult = await LoadUsableInvitationAsync(request.Token);
        if (!invitationResult.Success)
            return OperationResult<string>.Fail(invitationResult.ErrorMessage!);
        var invitation = invitationResult.Data!;

        if (!InvitationMatchesEmail(invitation, request.Email))
            return OperationResult<string>.Fail("Diese Einladung gilt nur für eine andere E-Mail-Adresse.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return OperationResult<string>.Fail(
                "Für diese E-Mail-Adresse existiert bereits ein Konto. Bitte melde dich an, um die Einladung anzunehmen.");

        var user = new TUser
        {
            FullName = request.FullName,
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return OperationResult<string>.Fail(string.Join(", ", createResult.Errors.Select(e => e.Description)));

        // Frisch angelegter Account – es gibt nichts Privates, das zurückbleiben könnte.
        await AddMemberAsync(invitation, user.Id, transferResources: true);

        // Passwort-Reset-Token zurückgeben, damit der Nutzer direkt zu change-password weitergeleitet wird.
        // Unbedenklich, da der Account in diesem Request mit dem Passwort des Aufrufers angelegt wurde.
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(resetToken);
        var encodedEmail = Uri.EscapeDataString(user.Email!);

        return OperationResult<string>.Ok($"reset-password?email={encodedEmail}&token={encodedToken}&invited=true");
    }

    /// <summary>
    /// Eingeloggter Flow: der angemeldete Nutzer <paramref name="userId"/> tritt dem Team bei. Die Identität
    /// kommt ausschließlich aus der Session, nie aus dem Request. <paramref name="transferResources"/>
    /// entscheidet, ob seine bisherigen Ressourcen ins Team wandern oder privat (und bis zum Austritt
    /// eingefroren) bleiben.
    /// </summary>
    public async Task<OperationResult> AcceptInvitationForUserAsync(string userId, string token, bool transferResources)
    {
        var invitationResult = await LoadUsableInvitationAsync(token);
        if (!invitationResult.Success)
            return OperationResult.Fail(invitationResult.ErrorMessage!);
        var invitation = invitationResult.Data!;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("Nutzer nicht gefunden.");

        if (!InvitationMatchesEmail(invitation, user.Email))
            return OperationResult.Fail("Diese Einladung gilt nur für eine andere E-Mail-Adresse.");

        if (await _db.Teams.AnyAsync(t => t.AdminUserId == userId))
            return OperationResult.Fail("Du bist Admin eines eigenen Teams und kannst keinem anderen Team beitreten.");

        if (await _db.TeamMembers.AnyAsync(m => m.UserId == userId))
            return OperationResult.Fail("Du bist bereits Mitglied eines Teams.");

        await AddMemberAsync(invitation, userId, transferResources);
        return OperationResult.Ok();
    }

    private async Task<OperationResult<TeamInvitation>> LoadUsableInvitationAsync(string token)
    {
        var invitation = await _db.TeamInvitations
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation?.Team == null)
            return OperationResult<TeamInvitation>.Fail("Einladung nicht gefunden.");

        if (invitation.IsRevoked)
            return OperationResult<TeamInvitation>.Fail("Diese Einladung wurde widerrufen.");

        if (invitation.ExpiresAt.HasValue && DateTime.UtcNow > invitation.ExpiresAt.Value)
            return OperationResult<TeamInvitation>.Fail("Diese Einladung ist abgelaufen.");

        if (invitation.MaxUses.HasValue && invitation.UsedCount >= invitation.MaxUses.Value)
            return OperationResult<TeamInvitation>.Fail("Das Nutzungslimit dieser Einladung wurde erreicht.");

        return OperationResult<TeamInvitation>.Ok(invitation);
    }

    // E-Mail-spezifische Einladung: Adresse muss übereinstimmen
    private static bool InvitationMatchesEmail(TeamInvitation invitation, string? email) =>
        string.IsNullOrEmpty(invitation.Email) ||
        string.Equals(invitation.Email, email?.Trim(), StringComparison.OrdinalIgnoreCase);

    private async Task AddMemberAsync(TeamInvitation invitation, string userId, bool transferResources)
    {
        foreach (var hook in _teamLifecycleHooks)
            await hook.OnMemberJoiningAsync(invitation.TeamId, userId, transferResources);

        _db.TeamMembers.Add(new TeamMember
        {
            TeamId = invitation.TeamId,
            UserId = userId,
            Role = TeamMemberRole.Member
        });
        invitation.UsedCount++;
        await _db.SaveChangesAsync();

        foreach (var hook in _teamLifecycleHooks)
            await hook.OnMemberJoinedAsync(invitation.TeamId, userId, transferResources);

        // Behält der Nutzer seine Ressourcen privat, bleiben auch seine persönlichen Keys im persönlichen
        // Scope – sie ruhen, solange er im Team ist (der Host lässt nur Keys des aktuellen Scopes zu).
        if (transferResources)
            await PersonalAccessTokenScope.MoveAsync(_authDb, userId, userId, invitation.TeamId);
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
            title: "Team Invitation",
            intro: $"You have been invited to join the team {teamName}.",
            actionLabel: "Accept invitation",
            actionUrl: inviteLink,
            outro: "If you were not expecting this invitation, you can safely ignore this email.");

        await _emailSender.SendEmailAsync(email, $"Invitation to team: {teamName}", html);
    }
}
