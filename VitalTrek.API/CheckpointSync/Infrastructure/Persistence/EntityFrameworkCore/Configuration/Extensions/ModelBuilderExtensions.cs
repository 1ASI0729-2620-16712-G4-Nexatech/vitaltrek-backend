using Microsoft.EntityFrameworkCore;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Model.Entities;
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

        // Sync Batches
        builder.Entity<SyncBatch>().HasKey(b => b.Id);
        builder.Entity<SyncBatch>().Property(b => b.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<SyncBatch>().Property(b => b.ClientBatchId).IsRequired().HasColumnType("char(36)");
        builder.Entity<SyncBatch>().Property(b => b.ContentFingerprint).IsRequired()
            .HasColumnType($"char({SyncBatch.FingerprintLength})");
        builder.Entity<SyncBatch>().Property(b => b.ReceivedAt)
            .HasConversion(value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired().HasPrecision(6);
        builder.Entity<SyncBatch>().Property(b => b.EvaluationStatus)
            .HasConversion(status => status.ToCode(), code => SyncBatchCodeExtensions.FromCode<SyncBatchEvaluationStatus>(code))
            .IsRequired().HasMaxLength(20);
        // The batch identity is scoped to its session; a concurrent duplicate fails here and is retried as a replay.
        builder.Entity<SyncBatch>().HasIndex(b => new { b.SessionId, b.ClientBatchId }).IsUnique()
            .HasDatabaseName("ux_sync_batches_session_client_batch");
        builder.Entity<SyncBatch>().HasIndex(b => b.AgencyId);
        builder.Entity<SyncBatch>().HasMany(b => b.Records).WithOne()
            .HasForeignKey(r => r.SyncBatchId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SyncBatch>().Navigation(b => b.Records).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Entity<SyncBatchRecord>().HasKey(r => r.Id);
        builder.Entity<SyncBatchRecord>().Property(r => r.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<SyncBatchRecord>().Property(r => r.ClientRecordId).IsRequired().HasColumnType("char(36)");
        builder.Entity<SyncBatchRecord>().Property(r => r.Kind)
            .HasConversion(kind => kind.ToCode(), code => SyncBatchCodeExtensions.FromCode<SyncRecordKind>(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<SyncBatchRecord>().Property(r => r.Outcome)
            .HasConversion(outcome => outcome.ToCode(), code => SyncBatchCodeExtensions.FromCode<SyncRecordOutcome>(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<SyncBatchRecord>().Property(r => r.RejectionCode)
            .HasMaxLength(SyncBatchRecord.MaximumRejectionCodeLength);
        builder.Entity<SyncBatchRecord>().HasIndex(r => new { r.SyncBatchId, r.ClientRecordId }).IsUnique()
            .HasDatabaseName("ux_sync_batch_records_batch_client_record");
    }
}
