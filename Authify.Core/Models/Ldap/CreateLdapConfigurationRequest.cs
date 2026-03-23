using System.ComponentModel.DataAnnotations;

namespace Authify.Core.Models.Ldap;

public class CreateLdapConfigurationRequest
{
    [Required]
    public string Domain { get; set; } = string.Empty;

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 389;

    public bool UseSsl { get; set; } = false;

    [Required]
    public string BindDn { get; set; } = string.Empty;

    [Required]
    public string BindPassword { get; set; } = string.Empty;

    [Required]
    public string BaseDn { get; set; } = string.Empty;

    public string SearchFilter { get; set; } = "(&(objectClass=user)(mail={0}))";
    public string EmailAttribute { get; set; } = "mail";
    public string DisplayNameAttribute { get; set; } = "displayName";
    public bool IsEnabled { get; set; } = true;
}
