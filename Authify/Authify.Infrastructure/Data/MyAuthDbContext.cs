using Authify.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Authify.Infrastructure.Data;

public class MyAuthDbContext : IdentityDbContext<ApplicationUser>
{
    public MyAuthDbContext(DbContextOptions<MyAuthDbContext> options)
        : base(options)
    {
    }
}