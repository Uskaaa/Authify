namespace Authify.Core.Models;

public class CreatePersonalAccessTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public PersonalAccessTokenDto Metadata { get; set; } = new();
}
