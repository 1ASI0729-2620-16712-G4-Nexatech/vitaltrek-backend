using VitalTrek.API.TourClosure.Domain.Model.Aggregates;
using VitalTrek.API.TourClosure.Interfaces.Rest.Resources;

namespace VitalTrek.API.TourClosure.Interfaces.Rest.Transform
{
    public static class TourSummaryResourceFromEntityAssembler
    {
        public static TourSummaryResource ToResourceFromEntity(TourSummary entity)
        {
            return new TourSummaryResource(
                entity.Id,
                entity.ExpeditionGroupId,
                entity.AgencyId,
                entity.StartedAt,
                entity.ClosedAt,
                entity.TotalParticipants,
                entity.CheckpointsReached,
                entity.AlertsRaised,
                entity.IncidentsReported,
                entity.Notes,
                entity.Status
            );
        }
    }
}