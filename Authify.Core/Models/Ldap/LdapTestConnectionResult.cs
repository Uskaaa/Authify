namespace Authify.Core.Models.Ldap;

public class LdapTestConnectionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Anzahl der gefundenen Einträge in der Suchbasis (Plausibilitätsprüfung).</summary>
    public int? EntryCount { get; set; }

    public static LdapTestConnectionResult Ok(int? entryCount = null) =>
        new() { Success = true, EntryCount = entryCount };

    public static LdapTestConnectionResult Fail(string error) =>
        new() { Success = false, ErrorMessage = error };
}
