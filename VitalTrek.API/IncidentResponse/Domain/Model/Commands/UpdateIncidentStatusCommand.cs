using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Commands;

/// <summary>
///     Command to move an incident to another state of its lifecycle.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="IncidentId">The incident identifier.</param>
/// <param name="Status">The target status.</param>
public record UpdateIncidentStatusCommand(int AgencyId, int IncidentId, IncidentStatus Status);
