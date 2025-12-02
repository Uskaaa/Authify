using Microsoft.AspNetCore.Identity;

namespace Authify.Application.Data;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}