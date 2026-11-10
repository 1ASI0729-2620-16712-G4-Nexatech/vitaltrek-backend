using Microsoft.EntityFrameworkCore;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
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
        builder.Entity<Incident>().HasKey(incident => incident.Id);
        builder.Entity<Incident>().Property(incident => incident.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Incident>().Property(incident => incident.SessionId).IsRequired();
        builder.Entity<Incident>().Property(incident => incident.AgencyId).IsRequired();
        builder.Entity<Incident>().Property(incident => incident.ReportedAt).IsRequired();
        builder.Entity<Incident>().Property(incident => incident.Description).IsRequired().HasMaxLength(500);
        builder.Entity<Incident>().Property(incident => incident.IncidentType)
            .HasConversion(type => type.ToCode(), code => IncidentTypeExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<Incident>().Property(incident => incident.Severity)
            .HasConversion(severity => severity.ToCode(), code => IncidentSeverityExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<Incident>().Property(incident => incident.Status)
            .HasConversion(status => status.ToCode(), code => IncidentStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<Incident>().HasIndex(incident => incident.AgencyId);
        builder.Entity<Incident>().HasIndex(incident => incident.SessionId);
        builder.Entity<Incident>().HasIndex(incident => new { incident.AgencyId, incident.Status });

        builder.Entity<IncidentNotification>().HasKey(notification => notification.Id);
        builder.Entity<IncidentNotification>().Property(notification => notification.Id).IsRequired()
            .ValueGeneratedOnAdd();
        builder.Entity<IncidentNotification>().Property(notification => notification.IncidentId).IsRequired();
        builder.Entity<IncidentNotification>().Property(notification => notification.AgencyId).IsRequired();
        builder.Entity<IncidentNotification>().Property(notification => notification.SentAt).IsRequired();
        builder.Entity<IncidentNotification>().Property(notification => notification.Channel).IsRequired()
            .HasMaxLength(30);
        builder.Entity<IncidentNotification>().Property(notification => notification.RecipientType)
            .HasConversion(recipientType => recipientType.ToCode(),
                code => NotificationRecipientTypeExtensions.FromCode(code))
            .IsRequired().HasMaxLength(30);
        builder.Entity<IncidentNotification>().Property(notification => notification.Status)
            .HasConversion(status => status.ToCode(), code => NotificationStatusExtensions.FromCode(code))
            .IsRequired().HasMaxLength(20);
        builder.Entity<IncidentNotification>().HasIndex(notification => notification.IncidentId);
        builder.Entity<IncidentNotification>().HasIndex(notification => notification.AgencyId);
    }
}
