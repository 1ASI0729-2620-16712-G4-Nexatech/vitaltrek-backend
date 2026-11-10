using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

namespace VitalTrek.API.IncidentResponse.Domain.Model.Commands;

/// <summary>
///     Command to declare an incident that occurred during an active tracking session.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="SessionId">The tracking session identifier.</param>
/// <param name="IncidentType">The nature of the incident.</param>
/// <param name="Severity">The severity assigned to the incident.</param>
/// <param name="Description">The description of what happened.</param>
/// <param name="ReportedAt">The moment the incident was reported.</param>
public record DeclareIncidentCommand(int AgencyId, int SessionId, IncidentType IncidentType, IncidentSeverity Severity,
    string Description, DateTimeOffset ReportedAt);
