using Authify.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Data;

public interface IAuthifyDbContext
{
    DbSet<UserTwoFactor> UserSubscriptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}