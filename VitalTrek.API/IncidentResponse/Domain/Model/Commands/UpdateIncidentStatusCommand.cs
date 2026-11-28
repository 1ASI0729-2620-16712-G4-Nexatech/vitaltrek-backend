using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Commands;

/// <summary>
///     Command to execute one explicit transition of the incident lifecycle: confirm, dismiss, declare an emergency,
///     resolve or close. The target status identifies the transition.
/// </summary>
/// <param name="AgencyId">The agency of the authenticated user.</param>
/// <param name="IncidentId">The incident identifier.</param>
/// <param name="Status">The status reached by the transition.</param>
/// <param name="Note">Optional note that justifies the transition.</param>
public record UpdateIncidentStatusCommand(int AgencyId, int IncidentId, IncidentStatus Status, string? Note = null);
