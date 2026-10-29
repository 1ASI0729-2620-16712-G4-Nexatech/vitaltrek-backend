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
            .HasIndex(window => new { window.RouteId, window.FromCheckpointId, window.ToCheckpointId }).IsUnique();
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
    }
}
