using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;

public partial class Incident : IAuditableEntity
{
    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }
}
