using Microsoft.EntityFrameworkCore;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.ValueObjects;

namespace VitalTrek.API.CheckpointSync.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Entity Framework Core model configuration for the Checkpoint Sync bounded context.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies the mapping of the Checkpoint Sync aggregates. No foreign keys point to other bounded contexts;
    ///     the identifiers are validated through anti-corruption layer facades.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ApplyCheckpointSyncConfiguration(this ModelBuilder builder)
    {
        builder.Entity<CheckpointPassage>().HasKey(p => p.Id);
        builder.Entity<CheckpointPassage>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<CheckpointPassage>().Property(p => p.PassedAt).IsRequired();
        builder.Entity<CheckpointPassage>().Property(p => p.SyncedAt).IsRequired();
        builder.Entity<CheckpointPassage>().Property(p => p.Status)
            .HasConversion(status => status.ToCode(), code => CheckpointPassageStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<CheckpointPassage>().HasIndex(p => new { p.SessionId, p.CheckpointId }).IsUnique()
            .HasDatabaseName("ux_checkpoint_passages_session_checkpoint");
        builder.Entity<CheckpointPassage>().HasIndex(p => p.AgencyId);
        builder.Entity<CheckpointPassage>().HasIndex(p => p.CheckpointId);
        builder.Entity<CheckpointPassage>().HasIndex(p => p.FieldGuideId);
    }
}
