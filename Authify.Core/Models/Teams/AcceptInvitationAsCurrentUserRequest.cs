using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models.Teams;

public class AcceptInvitationAsCurrentUserRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>true = bisherige Ressourcen ins Team übernehmen, false = privat behalten (bis zum Austritt eingefroren).</summary>
    public bool TransferResources { get; set; }
}
