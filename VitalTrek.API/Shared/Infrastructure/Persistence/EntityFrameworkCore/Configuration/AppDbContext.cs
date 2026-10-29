using Microsoft.EntityFrameworkCore;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;

namespace VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Represents the Entity Framework Core database context for the VitalTrek Platform API.
/// </summary>
/// <remarks>
///     Each bounded context contributes its own model configuration through a <see cref="ModelBuilder"/>
///     extension method, keeping the mapping of every context isolated from the others.
/// </remarks>
/// <param name="options">The options to be used by the <see cref="DbContext"/>.</param>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor());
        base.OnConfiguring(builder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Bounded context configurations are applied here, e.g. builder.ApplyIamConfiguration();

        builder.UseSnakeCaseNamingConvention();
    }
}
