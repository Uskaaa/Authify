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

    /// <summary>Called right after a team is created, with its admin already a member. Use to migrate any
    /// host-side resources the admin owned individually (scoped by their own user id) into team scope.</summary>
    Task OnTeamCreatedAsync(string teamId, string adminUserId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Called right after a user becomes a team member (invitation accepted, or added directly by an
    /// admin). Use to migrate any host-side resources the user owned individually into team scope.</summary>
    Task OnMemberJoinedAsync(string teamId, string memberUserId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
