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

    /// <summary>Called right after a user becomes a team member <b>and chose to bring their resources along</b>
    /// (always the case for brand-new accounts). Use to migrate any host-side resources the user owned
    /// individually into team scope.</summary>
    Task OnMemberJoinedAsync(string teamId, string memberUserId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Called right before a user becomes a team member — they still act in their personal scope
    /// here. <paramref name="transferResources"/> = false means the user keeps their individual resources
    /// private: they stay in the personal scope, invisible while the user acts in team scope, and are theirs
    /// again after leaving. Use to stop anything that would otherwise keep running unseen in the meantime.
    /// Throwing aborts the join.</summary>
    Task OnMemberJoiningAsync(string teamId, string memberUserId, bool transferResources, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Called right after a user becomes a team member. The default forwards to
    /// <see cref="OnMemberJoinedAsync(string, string, CancellationToken)"/> only when the user chose to
    /// transfer their resources.</summary>
    Task OnMemberJoinedAsync(string teamId, string memberUserId, bool transferResources, CancellationToken cancellationToken = default)
        => transferResources ? OnMemberJoinedAsync(teamId, memberUserId, cancellationToken) : Task.CompletedTask;
}
