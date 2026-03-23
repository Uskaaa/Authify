namespace Authify.Core.Interfaces;

public interface IUserRegistrationHook
{
    /// <summary>
    /// Wird aufgerufen, nachdem ein User erfolgreich registriert wurde.
    /// </summary>
    Task OnUserRegisteredAsync(string userId, string email);
}