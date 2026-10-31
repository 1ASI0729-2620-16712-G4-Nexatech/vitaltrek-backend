using Microsoft.EntityFrameworkCore;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Entity Framework Core model configuration for the Expedition Setup bounded context.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies the mapping of the Expedition Setup aggregates.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ApplyExpeditionSetupConfiguration(this ModelBuilder builder)
    {
        // Routes
        builder.Entity<Route>().HasKey(route => route.Id);
        builder.Entity<Route>().Property(route => route.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Route>().Property(route => route.Name).IsRequired().HasMaxLength(120);
        builder.Entity<Route>().Property(route => route.Origin).IsRequired().HasMaxLength(120);
        builder.Entity<Route>().Property(route => route.Destination).IsRequired().HasMaxLength(120);
        builder.Entity<Route>().Property(route => route.DistanceKm).IsRequired().HasPrecision(8, 2);
        builder.Entity<Route>().Property(route => route.Difficulty)
            .HasConversion(difficulty => difficulty.ToString(), code => RouteDifficultyExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<Route>().Property(route => route.Status)
            .HasConversion(status => status.ToCode(), code => RouteStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<Route>().Ignore(route => route.IsEnabled);
        builder.Entity<Route>().HasOne<Agency>().WithMany().HasForeignKey(route => route.AgencyId).IsRequired();
        builder.Entity<Route>().HasIndex(route => new { route.AgencyId, route.Name }).IsUnique();

        // Checkpoints
        builder.Entity<Checkpoint>().HasKey(checkpoint => checkpoint.Id);
        builder.Entity<Checkpoint>().Property(checkpoint => checkpoint.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Checkpoint>().Property(checkpoint => checkpoint.Location).IsRequired().HasMaxLength(120);
        builder.Entity<Checkpoint>().Property(checkpoint => checkpoint.BluetoothId).IsRequired().HasMaxLength(50);
        builder.Entity<Checkpoint>().HasOne<Route>().WithMany().HasForeignKey(checkpoint => checkpoint.RouteId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Checkpoint>().HasIndex(checkpoint => new { checkpoint.RouteId, checkpoint.SequenceOrder }).IsUnique();
        builder.Entity<Checkpoint>().HasIndex(checkpoint => checkpoint.BluetoothId).IsUnique();
        builder.Entity<Checkpoint>().HasIndex(checkpoint => checkpoint.AgencyId);

        // Expected time windows
        builder.Entity<ExpectedTimeWindow>().HasKey(window => window.Id);
        builder.Entity<ExpectedTimeWindow>().Property(window => window.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<ExpectedTimeWindow>().HasOne<Route>().WithMany().HasForeignKey(window => window.RouteId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ExpectedTimeWindow>().HasOne<Checkpoint>().WithMany()
            .HasForeignKey(window => window.FromCheckpointId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ExpectedTimeWindow>().HasOne<Checkpoint>().WithMany()
            .HasForeignKey(window => window.ToCheckpointId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ExpectedTimeWindow>()
            .HasIndex(window => new { window.RouteId, window.FromCheckpointId, window.ToCheckpointId }).IsUnique()
            .HasDatabaseName("ux_expected_time_windows_segment");
        builder.Entity<ExpectedTimeWindow>().HasIndex(window => window.AgencyId);

        // Route notes
        builder.Entity<RouteNote>().HasKey(note => note.Id);
        builder.Entity<RouteNote>().Property(note => note.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<RouteNote>().Property(note => note.Title).IsRequired().HasMaxLength(120);
        builder.Entity<RouteNote>().Property(note => note.Content).IsRequired().HasMaxLength(4000);
        builder.Entity<RouteNote>().HasOne<Route>().WithMany().HasForeignKey(note => note.RouteId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<RouteNote>().HasIndex(note => new { note.RouteId, note.DisplayOrder }).IsUnique();
        builder.Entity<RouteNote>().HasIndex(note => note.AgencyId);

        // Safety protocols
        builder.Entity<SafetyProtocol>().HasKey(protocol => protocol.Id);
        builder.Entity<SafetyProtocol>().Property(protocol => protocol.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<SafetyProtocol>().Property(protocol => protocol.Instructions).IsRequired().HasMaxLength(4000);
        builder.Entity<SafetyProtocol>().Property(protocol => protocol.Status)
            .HasConversion(status => status.ToCode(), code => SafetyProtocolStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<SafetyProtocol>().HasOne<Route>().WithMany().HasForeignKey(protocol => protocol.RouteId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SafetyProtocol>().HasIndex(protocol => protocol.RouteId).IsUnique();
        builder.Entity<SafetyProtocol>().HasIndex(protocol => protocol.AgencyId);

        // Field guides
        builder.Entity<FieldGuide>().HasKey(guide => guide.Id);
        builder.Entity<FieldGuide>().Property(guide => guide.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<FieldGuide>().Property(guide => guide.Name).IsRequired().HasMaxLength(120);
        builder.Entity<FieldGuide>().HasOne<Agency>().WithMany().HasForeignKey(guide => guide.AgencyId).IsRequired();

        // Expedition groups
        builder.Entity<ExpeditionGroup>().HasKey(group => group.Id);
        builder.Entity<ExpeditionGroup>().Property(group => group.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<ExpeditionGroup>().Property(group => group.Name).IsRequired().HasMaxLength(120);
        builder.Entity<ExpeditionGroup>().Property(group => group.DepartureDate)
            .HasConversion(date => date.ToDateTime(TimeOnly.MinValue), value => DateOnly.FromDateTime(value))
            .HasColumnType("date").IsRequired();
        builder.Entity<ExpeditionGroup>().Property(group => group.Status)
            .HasConversion(status => status.ToCode(), code => ExpeditionGroupStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<ExpeditionGroup>().Ignore(group => group.IsScheduled);
        builder.Entity<ExpeditionGroup>().HasOne<Agency>().WithMany().HasForeignKey(group => group.AgencyId)
            .IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ExpeditionGroup>().HasOne<Route>().WithMany().HasForeignKey(group => group.RouteId)
            .IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ExpeditionGroup>().HasOne<FieldGuide>().WithMany().HasForeignKey(group => group.FieldGuideId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ExpeditionGroup>().HasIndex(group => new { group.FieldGuideId, group.DepartureDate });

        // Tourist manifest
        builder.Entity<ManifestEntry>().HasKey(entry => entry.Id);
        builder.Entity<ManifestEntry>().Property(entry => entry.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<ManifestEntry>().Property(entry => entry.FullName).IsRequired().HasMaxLength(120);
        builder.Entity<ManifestEntry>().Property(entry => entry.IdentityDocument).IsRequired().HasMaxLength(20);
        builder.Entity<ManifestEntry>().HasOne<ExpeditionGroup>().WithMany().HasForeignKey(entry => entry.ExpeditionGroupId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ManifestEntry>().HasIndex(entry => new { entry.ExpeditionGroupId, entry.IdentityDocument })
            .IsUnique().HasDatabaseName("ux_manifest_entries_group_document");
        builder.Entity<ManifestEntry>().HasIndex(entry => new { entry.AgencyId, entry.IdentityDocument });

        // Wearable inventory
        builder.Entity<WearableDevice>().HasKey(device => device.Id);
        builder.Entity<WearableDevice>().Property(device => device.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<WearableDevice>().Property(device => device.Code).IsRequired().HasMaxLength(40);
        builder.Entity<WearableDevice>().Property(device => device.Status)
            .HasConversion(status => status.ToCode(), code => WearableDeviceStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<WearableDevice>().Ignore(device => device.IsAvailable);
        builder.Entity<WearableDevice>().HasOne<Agency>().WithMany().HasForeignKey(device => device.AgencyId).IsRequired();
        builder.Entity<WearableDevice>().HasIndex(device => device.Code).IsUnique();

        // Participant preparations
        builder.Entity<ParticipantPreparation>().HasKey(preparation => preparation.Id);
        builder.Entity<ParticipantPreparation>().Property(preparation => preparation.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<ParticipantPreparation>().Property(preparation => preparation.EmergencyContactName).HasMaxLength(120);
        builder.Entity<ParticipantPreparation>().Property(preparation => preparation.EmergencyContactPhone).HasMaxLength(25);
        builder.Entity<ParticipantPreparation>().Property(preparation => preparation.EmergencyContactRelationship).HasMaxLength(60);
        builder.Entity<ParticipantPreparation>().Property(preparation => preparation.ReturnCondition)
            .HasConversion(condition => condition.HasValue ? condition.Value.ToCode() : null,
                code => code == null ? null : WearableDeviceStatusExtensions.FromCode(code))
            .HasMaxLength(20);
        builder.Entity<ParticipantPreparation>().Ignore(preparation => preparation.EmergencyContact);
        builder.Entity<ParticipantPreparation>().Ignore(preparation => preparation.IsReady);
        builder.Entity<ParticipantPreparation>().HasOne<ExpeditionGroup>().WithMany()
            .HasForeignKey(preparation => preparation.ExpeditionGroupId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ParticipantPreparation>().HasOne<ManifestEntry>().WithMany()
            .HasForeignKey(preparation => preparation.MemberId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ParticipantPreparation>().HasOne<WearableDevice>().WithMany()
            .HasForeignKey(preparation => preparation.WearableId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ParticipantPreparation>().HasIndex(preparation => preparation.MemberId).IsUnique();
    }
}
