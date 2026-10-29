using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;


namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>
///     Application query service interface for safety protocol read operations.
/// </summary>
public interface ISafetyProtocolQueryService
{
    /// <summary>Retrieves the safety protocols of an agency, optionally filtered by route.</summary>
    Task<IEnumerable<SafetyProtocol>> Handle(GetAllSafetyProtocolsQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves a safety protocol that belongs to an agency.</summary>
    Task<SafetyProtocol?> Handle(GetSafetyProtocolByIdQuery query, CancellationToken cancellationToken);
}
