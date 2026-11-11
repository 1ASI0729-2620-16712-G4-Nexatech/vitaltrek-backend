namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;

/// <summary>
///     Command to compare the checkpoint passages of a tracking session against the expected time windows of its
///     route and raise a delay alert for every segment whose maximum time was exceeded.
/// </summary>
/// <param name="AgencyId">The agency identifier.</param>
/// <param name="SessionId">The tracking session identifier.</param>
public record EvaluateSessionDelaysCommand(int AgencyId, int SessionId);
