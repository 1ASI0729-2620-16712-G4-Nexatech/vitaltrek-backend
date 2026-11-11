using Microsoft.EntityFrameworkCore;
using VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

namespace VitalTrek.API.SafetyMonitoring.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Entity Framework Core model configuration for the Safety Monitoring bounded context.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies the mapping of the Safety Monitoring aggregates. No foreign keys point to other bounded contexts;
    ///     the identifiers are validated through anti-corruption layer facades.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ApplySafetyMonitoringConfiguration(this ModelBuilder builder)
    {
        builder.Entity<EarlyWarningAlert>().HasKey(a => a.Id);
        builder.Entity<EarlyWarningAlert>().Property(a => a.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<EarlyWarningAlert>().Property(a => a.AlertType)
            .HasConversion(type => type.ToCode(), code => AlertTypeExtensions.FromCode(code))
            .IsRequired().HasMaxLength(30);
        builder.Entity<EarlyWarningAlert>().Property(a => a.Severity)
            .HasConversion(severity => severity.ToCode(), code => AlertSeverityExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<EarlyWarningAlert>().Property(a => a.Status)
            .HasConversion(status => status.ToCode(), code => AlertStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<EarlyWarningAlert>().Property(a => a.Description).IsRequired()
            .HasMaxLength(EarlyWarningAlert.DescriptionMaxLength);
        builder.Entity<EarlyWarningAlert>().Property(a => a.RaisedAt).IsRequired();
        builder.Entity<EarlyWarningAlert>().HasIndex(a => new { a.AgencyId, a.Status });
        builder.Entity<EarlyWarningAlert>().HasIndex(a => new { a.SessionId, a.CheckpointId, a.AlertType })
            .HasDatabaseName("ix_early_warning_alerts_session_checkpoint_type");
        builder.Entity<EarlyWarningAlert>().HasIndex(a => a.ExpeditionGroupId);
    }
}
