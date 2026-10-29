using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;


namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application query service interface for expected time window read operations.
/// </summary>
public interface IExpectedTimeWindowQueryService
{
    /// <summary>Retrieves the expected time windows of an agency, optionally filtered by route.</summary>
    Task<IEnumerable<ExpectedTimeWindow>> Handle(GetAllExpectedTimeWindowsQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves an expected time window that belongs to an agency.</summary>
    Task<ExpectedTimeWindow?> Handle(GetExpectedTimeWindowByIdQuery query, CancellationToken cancellationToken);
}
