using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.IAM.Domain.Model.Aggregates;

public partial class Agency : IAuditableEntity
{
    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }
}
