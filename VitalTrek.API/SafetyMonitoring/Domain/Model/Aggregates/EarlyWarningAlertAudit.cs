using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;

public partial class EarlyWarningAlert : IAuditableEntity
{
    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }
}
