using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;

public partial class CheckpointPassage : IAuditableEntity
{
    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }
}
