using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;

public partial class IncidentNotification : IAuditableEntity
{
    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }
}
