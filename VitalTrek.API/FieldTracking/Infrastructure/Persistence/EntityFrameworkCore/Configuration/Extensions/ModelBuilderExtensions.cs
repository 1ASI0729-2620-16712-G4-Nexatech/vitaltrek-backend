using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Domain.Model.Aggregates;

namespace VitalTrek.API.FieldTracking.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Entity Framework Core model configuration for the Field Tracking bounded context.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies the mapping of the Field Tracking aggregates.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ApplyFieldTrackingConfiguration(this ModelBuilder builder)
    {
        // Tracking Sessions
        builder.Entity<TrackingSession>().HasKey(s => s.Id);
        builder.Entity<TrackingSession>().Property(s => s.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<TrackingSession>().Property(s => s.Status)
            .HasConversion(status => status.ToCode(), code => SessionStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<TrackingSession>().Property(s => s.StartedAt).IsRequired();
        builder.Entity<TrackingSession>().Ignore(s => s.IsActive);
        builder.Entity<TrackingSession>().HasOne<Agency>().WithMany().HasForeignKey(s => s.AgencyId).IsRequired();
        builder.Entity<TrackingSession>().HasOne<ExpeditionGroup>().WithMany()
            .HasForeignKey(s => s.ExpeditionGroupId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TrackingSession>().HasIndex(s => s.AgencyId);
        builder.Entity<TrackingSession>().HasIndex(s => new { s.ExpeditionGroupId, s.Status })
            .HasDatabaseName("ix_tracking_sessions_group_status");

        // Telemetry Records
        builder.Entity<TelemetryRecord>().HasKey(r => r.Id);
        builder.Entity<TelemetryRecord>().Property(r => r.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<TelemetryRecord>().Property(r => r.Latitude).IsRequired().HasPrecision(10, 7);
        builder.Entity<TelemetryRecord>().Property(r => r.Longitude).IsRequired().HasPrecision(10, 7);
        builder.Entity<TelemetryRecord>().Property(r => r.Altitude).IsRequired().HasPrecision(10, 2);
        builder.Entity<TelemetryRecord>().Property(r => r.RecordedAt).IsRequired();
        builder.Entity<TelemetryRecord>().Property(r => r.SyncedAt).IsRequired();
        builder.Entity<TelemetryRecord>().HasOne<TrackingSession>().WithMany()
            .HasForeignKey(r => r.SessionId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TelemetryRecord>().HasIndex(r => r.SessionId);
        builder.Entity<TelemetryRecord>().HasIndex(r => new { r.SessionId, r.WearableDeviceId, r.RecordedAt })
            .HasDatabaseName("ix_telemetry_records_session_wearable_time");
    }
}
