using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.FieldTracking.Domain.Model.Aggregates;

/// <summary>
///     Audit properties for <see cref="TelemetryRecord"/>.
/// </summary>
public partial class TelemetryRecord : IAuditableEntity
{
    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }
}
