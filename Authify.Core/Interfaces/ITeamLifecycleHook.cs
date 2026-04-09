namespace Authify.Core.Interfaces;

/// <summary>
/// Optional host/module hooks for team lifecycle events.
/// </summary>
public interface ITeamLifecycleHook
{
    Task OnTeamDeletingAsync(string teamId, string adminUserId, CancellationToken cancellationToken = default);
}
