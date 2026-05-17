namespace Authify.Core.Interfaces;

/// <summary>
/// Optional host/module hooks for team lifecycle events.
/// </summary>
public interface ITeamLifecycleHook
{
    Task OnTeamDeletingAsync(string teamId, string adminUserId, CancellationToken cancellationToken = default);

    /// <summary>Called before a team member's record is deleted. Use to clean up any host-side data tied to the member.</summary>
    Task OnMemberRemovingAsync(string teamId, string memberUserId, CancellationToken cancellationToken = default)
        => Task.CompletedTask; // default no-op so existing implementations don't break
}
