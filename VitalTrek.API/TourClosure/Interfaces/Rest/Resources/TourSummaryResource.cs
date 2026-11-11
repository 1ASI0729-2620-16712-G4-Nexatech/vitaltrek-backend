using System;

namespace VitalTrek.API.TourClosure.Interfaces.Rest.Resources
{
    public record TourSummaryResource(
        int Id,
        int ExpeditionGroupId,
        int AgencyId,
        DateTimeOffset StartedAt,
        DateTimeOffset? ClosedAt,
        int TotalParticipants,
        int CheckpointsReached,
        int AlertsRaised,
        int IncidentsReported,
        string Notes,
        string Status
    );
}