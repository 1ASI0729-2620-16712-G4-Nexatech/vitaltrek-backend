using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.FieldTracking.Domain.Model.Aggregates;

/// <summary>
///     Audit properties for <see cref="TrackingSession"/>.
/// </summary>
public partial class TrackingSession : IAuditableEntity
{
    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }
}
