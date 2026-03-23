namespace Authify.Core.Models;

public class EmailConfirmationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}