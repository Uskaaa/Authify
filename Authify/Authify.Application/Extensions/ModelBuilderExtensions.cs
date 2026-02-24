using Authify.Application.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Authify.Application.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies all Authify entity configurations (e.g. composite keys).
    /// Call this in your DbContext's OnModelCreating:
    /// <code>modelBuilder.ApplyAuthifyConfigurations();</code>
    /// </summary>
    public static ModelBuilder ApplyAuthifyConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserTwoFactorConfiguration());
        return modelBuilder;
    }
}
