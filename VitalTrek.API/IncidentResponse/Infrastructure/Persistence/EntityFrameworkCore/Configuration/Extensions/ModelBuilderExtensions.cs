using Microsoft.EntityFrameworkCore;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Entities;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Entity Framework Core model configuration for the Incident Response bounded context.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies the mapping of the Incident Response aggregates. No foreign keys point to other bounded contexts;
    ///     the identifiers are validated through anti-corruption layer facades.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ApplyIncidentResponseConfiguration(this ModelBuilder builder)
    {
        ApplyIncident(builder);
        ApplyEvacuationAction(builder);
        ApplyNotification(builder);
    }

    private static void ApplyIncident(ModelBuilder builder)
    {
        var incident = builder.Entity<Incident>();
        incident.HasKey(entity => entity.Id);
        incident.Property(entity => entity.Id).IsRequired().ValueGeneratedOnAdd();
        incident.Property(entity => entity.SessionId).IsRequired();
        incident.Property(entity => entity.ExpeditionGroupId).IsRequired();
        incident.Property(entity => entity.AgencyId).IsRequired();
        incident.Property(entity => entity.ReportedAt).IsRequired();
        incident.Property(entity => entity.Description).IsRequired().HasMaxLength(Incident.MaximumDescriptionLength);
        incident.Property(entity => entity.IncidentType)
            .HasConversion(type => type.ToCode(), code => IncidentTypeExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        incident.Property(entity => entity.Severity)
            .HasConversion(severity => severity.ToCode(), code => IncidentSeverityExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        // The status is the concurrency token: two transitions of the same incident cannot both commit.
        incident.Property(entity => entity.Status)
            .HasConversion(status => status.ToCode(), code => IncidentStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(30).IsConcurrencyToken();
        incident.Ignore(entity => entity.IsOpen);
        incident.HasIndex(entity => entity.AgencyId);
        incident.HasIndex(entity => entity.SessionId);
        incident.HasIndex(entity => new { entity.AgencyId, entity.Status });
        incident.HasIndex(entity => new { entity.AgencyId, entity.ExpeditionGroupId });
        // One incident per alert; manual incidents have no alert and MySQL admits several null values.
        incident.HasIndex(entity => new { entity.AgencyId, entity.AlertId }).IsUnique()
            .HasDatabaseName("ux_incidents_agency_alert");
        incident.HasMany(entity => entity.StatusChanges).WithOne()
            .HasForeignKey(change => change.IncidentId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        incident.Navigation(entity => entity.StatusChanges).HasField("_statusChanges")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        var change = builder.Entity<IncidentStatusChange>();
        change.HasKey(entity => entity.Id);
        change.Property(entity => entity.Id).IsRequired().ValueGeneratedOnAdd();
        change.Property(entity => entity.FromStatus)
            .HasConversion(status => status.HasValue ? status.Value.ToCode() : null,
                code => code == null ? null : IncidentStatusExtensions.FromCode(code))
            .HasMaxLength(30);
        change.Property(entity => entity.ToStatus)
            .HasConversion(status => status.ToCode(), code => IncidentStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(30);
        change.Property(entity => entity.ActorUserId).IsRequired();
        change.Property(entity => entity.Note).HasMaxLength(IncidentStatusChange.MaximumNoteLength);
        change.Property(entity => entity.OccurredAt).IsRequired();
    }

    private static void ApplyEvacuationAction(ModelBuilder builder)
    {
        var action = builder.Entity<EvacuationAction>();
        action.HasKey(entity => entity.Id);
        action.Property(entity => entity.Id).IsRequired().ValueGeneratedOnAdd();
        action.Property(entity => entity.AgencyId).IsRequired();
        action.Property(entity => entity.IncidentId).IsRequired();
        action.Property(entity => entity.ExpeditionGroupId).IsRequired();
        action.Property(entity => entity.ClientActionId).IsRequired();
        action.Property(entity => entity.RecordedByUserId).IsRequired();
        action.Property(entity => entity.RecordedAt).IsRequired().HasColumnType("datetime(6)");
        action.Property(entity => entity.SynchronizedAt).IsRequired().HasColumnType("datetime(6)");
        action.Property(entity => entity.Description).IsRequired().HasMaxLength(EvacuationAction.MaximumDescriptionLength);
        action.Property(entity => entity.Location).HasMaxLength(EvacuationAction.MaximumLocationLength);
        action.Property(entity => entity.Status)
            .HasConversion(status => status.ToCode(), code => EvacuationActionStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        action.Property(entity => entity.ContentFingerprint).IsRequired().HasMaxLength(64).IsFixedLength();
        action.HasIndex(entity => new { entity.AgencyId, entity.ClientActionId }).IsUnique()
            .HasDatabaseName("ux_evacuation_actions_agency_client_action");
        action.HasIndex(entity => entity.IncidentId);
        action.HasOne<Incident>().WithMany().HasForeignKey(entity => entity.IncidentId).IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ApplyNotification(ModelBuilder builder)
    {
        var notification = builder.Entity<IncidentNotification>();
        notification.HasKey(entity => entity.Id);
        notification.Property(entity => entity.Id).IsRequired().ValueGeneratedOnAdd();
        notification.Property(entity => entity.NotificationId).IsRequired();
        notification.Property(entity => entity.IncidentId).IsRequired();
        notification.Property(entity => entity.AgencyId).IsRequired();
        notification.Property(entity => entity.RequestedAt).IsRequired();
        notification.Property(entity => entity.Channel).IsRequired().HasMaxLength(IncidentNotification.MaximumChannelLength);
        notification.Property(entity => entity.RecipientType)
            .HasConversion(recipientType => recipientType.ToCode(),
                code => NotificationRecipientTypeExtensions.FromCode(code))
            .IsRequired().HasMaxLength(30);
        notification.Property(entity => entity.RecipientAddress).HasMaxLength(IncidentNotification.MaximumAddressLength);
        notification.Property(entity => entity.Subject).HasMaxLength(IncidentNotification.MaximumSubjectLength);
        notification.Property(entity => entity.Body).HasMaxLength(IncidentNotification.MaximumBodyLength);
        notification.Property(entity => entity.PayloadFingerprint).HasMaxLength(64).IsFixedLength();
        notification.Property(entity => entity.Status)
            .HasConversion(status => status.ToCode(), code => NotificationStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        // The attempt counter is the concurrency token: two requests cannot start an attempt of the same notification.
        notification.Property(entity => entity.AttemptCount).IsRequired().IsConcurrencyToken();
        notification.Property(entity => entity.LastErrorCode).HasMaxLength(IncidentNotification.MaximumCodeLength);
        notification.Property(entity => entity.FailureCode).HasMaxLength(IncidentNotification.MaximumCodeLength);
        notification.Property(entity => entity.ProviderMessageId)
            .HasMaxLength(IncidentNotification.MaximumProviderMessageIdLength);
        notification.HasIndex(entity => new { entity.AgencyId, entity.NotificationId }).IsUnique()
            .HasDatabaseName("ux_incident_notifications_agency_notification");
        notification.HasIndex(entity => entity.IncidentId);
        notification.HasIndex(entity => entity.AgencyId);
    }
}
