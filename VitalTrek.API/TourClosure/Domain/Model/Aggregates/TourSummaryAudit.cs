using System;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.TourClosure.Domain.Model.Aggregates
{
    public partial class TourSummary : IAuditableEntity
    {
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}