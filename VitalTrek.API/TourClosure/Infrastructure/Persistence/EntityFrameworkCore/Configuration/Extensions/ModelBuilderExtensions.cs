using Microsoft.EntityFrameworkCore;
using VitalTrek.API.TourClosure.Domain.Model.Aggregates;

namespace VitalTrek.API.TourClosure.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void ApplyTourClosureConfiguration(this ModelBuilder builder)
        {
            builder.Entity<TourSummary>(entity =>
            {
                entity.ToTable("tour_summaries");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.ExpeditionGroupId).IsRequired();
                entity.Property(e => e.AgencyId).IsRequired();
                entity.Property(e => e.StartedAt).IsRequired();
                entity.Property(e => e.ClosedAt);
                entity.Property(e => e.TotalParticipants).IsRequired();
                entity.Property(e => e.CheckpointsReached).IsRequired();
                entity.Property(e => e.AlertsRaised).IsRequired();
                entity.Property(e => e.IncidentsReported).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            });
        }
    }
}