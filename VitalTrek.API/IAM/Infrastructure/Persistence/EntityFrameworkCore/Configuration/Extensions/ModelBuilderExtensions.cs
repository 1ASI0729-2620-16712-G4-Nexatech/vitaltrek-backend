using Microsoft.EntityFrameworkCore;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;

namespace VitalTrek.API.IAM.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Entity Framework Core model configuration for the IAM bounded context.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies the mapping of the IAM aggregates.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    /// <summary>Name of the unique index that links a field guide profile to a single account.</summary>
    public const string LinkedFieldGuideIndexName = "ux_users_agency_field_guide";

    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Agency>().HasKey(agency => agency.Id);
        builder.Entity<Agency>().Property(agency => agency.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Agency>().Property(agency => agency.Name).IsRequired().HasMaxLength(150);

        builder.Entity<User>().HasKey(user => user.Id);
        builder.Entity<User>().Property(user => user.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<User>().Property(user => user.Username)
            .HasConversion(valueObject => valueObject.Value, value => new Username(value))
            .IsRequired()
            .HasMaxLength(120);
        builder.Entity<User>().Property(user => user.PasswordHash).IsRequired().HasMaxLength(100);
        builder.Entity<User>().Property(user => user.Name).IsRequired().HasMaxLength(120);
        builder.Entity<User>().Property(user => user.Role)
            .HasConversion(role => role.ToCode(), code => RoleExtensions.FromCode(code))
            .IsRequired()
            .HasMaxLength(30);
        builder.Entity<User>().Property(user => user.IdentityDocument).HasMaxLength(20);
        builder.Entity<User>().HasOne<Agency>().WithMany().HasForeignKey(user => user.AgencyId).IsRequired();
        builder.Entity<User>().HasIndex(user => user.Username).IsUnique();
        // A field guide profile is linked to at most one account; tourists have no profile and MySQL admits null values.
        builder.Entity<User>().HasIndex(user => new { user.AgencyId, user.FieldGuideId }).IsUnique()
            .HasDatabaseName(LinkedFieldGuideIndexName);
        builder.Entity<User>().HasIndex(user => new { user.AgencyId, user.IdentityDocument });
    }
}
