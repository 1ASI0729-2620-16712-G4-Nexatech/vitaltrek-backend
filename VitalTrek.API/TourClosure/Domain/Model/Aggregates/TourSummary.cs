using System;
using VitalTrek.API.TourClosure.Domain.Model.Commands;

namespace VitalTrek.API.TourClosure.Domain.Model.Aggregates
{
    public partial class TourSummary
    {
        public int Id { get; private set; }
        public int ExpeditionGroupId { get; private set; }
        public int AgencyId { get; private set; }
        public DateTimeOffset StartedAt { get; private set; }
        public DateTimeOffset? ClosedAt { get; private set; }
        public int TotalParticipants { get; private set; }
        public int CheckpointsReached { get; private set; }
        public int AlertsRaised { get; private set; }
        public int IncidentsReported { get; private set; }
        public string Notes { get; private set; }
        public string Status { get; private set; } // "Draft" o "Published"

        public TourSummary()
        {
            Notes = string.Empty;
            Status = "Draft";
        }

        public TourSummary(
            int expeditionGroupId,
            int agencyId,
            DateTimeOffset startedAt,
            DateTimeOffset closedAt,
            int totalParticipants,
            int checkpointsReached,
            int alertsRaised,
            int incidentsReported,
            string notes)
        {
            ExpeditionGroupId = expeditionGroupId;
            AgencyId = agencyId;
            StartedAt = startedAt;
            ClosedAt = closedAt;
            TotalParticipants = totalParticipants;
            CheckpointsReached = checkpointsReached;
            AlertsRaised = alertsRaised;
            IncidentsReported = incidentsReported;
            Notes = notes ?? string.Empty;
            Status = "Draft";
        }

        public void Publish()
        {
            Status = "Published";
        }

        public void UpdateNotes(string notes)
        {
            Notes = notes ?? string.Empty;
        }
    }
}