using System;

namespace VitalTrek.API.TourClosure.Interfaces.Rest.Resources
{
    public record CloseTourResource(
        int ExpeditionGroupId,
        int AgencyId,
        DateTimeOffset StartedAt,
        DateTimeOffset ClosedAt,
        int TotalParticipants,
        int CheckpointsReached,
        int AlertsRaised,
        int IncidentsReported,
        string Notes
    );
}