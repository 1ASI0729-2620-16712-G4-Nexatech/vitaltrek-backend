namespace VitalTrek.API.SafetyMonitoring.Interfaces.ACL.Resources;

/// <summary>
///     Minimal, identifier-only view of an early warning alert for other bounded contexts. It carries no evidence, member
///     or health data; Incident Response uses it to verify that an alert belongs to the session and group of an incident.
/// </summary>
/// <param name="AlertId">The alert identifier.</param>
/// <param name="SessionId">The tracking session of the alert.</param>
/// <param name="ExpeditionGroupId">The expedition group of the alert.</param>
/// <param name="StatusCode">The external status code: active, acknowledged, resolved or dismissed.</param>
public sealed record AlertReference(int AlertId, int SessionId, int ExpeditionGroupId, string StatusCode);
