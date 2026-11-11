using System;

namespace VitalTrek.API.TourClosure.Domain.Model.Commands
{
    public record CloseTourCommand(
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