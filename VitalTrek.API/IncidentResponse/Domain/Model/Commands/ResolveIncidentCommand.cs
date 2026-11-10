namespace VitalTrek.API.IncidentResponse.Domain.Model.Commands;

/// <summary>
///     Command to resolve an open incident.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="IncidentId">The incident identifier.</param>
public record ResolveIncidentCommand(int AgencyId, int IncidentId);
