namespace VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;

/// <summary>
///     Heart rate baseline of a participant: the only preparation data Safety Monitoring needs to evaluate persisted
///     readings. It contains no contact, consent or other health details.
/// </summary>
/// <param name="MemberId">The manifest entry of the participant, as recorded in the telemetry readings.</param>
/// <param name="BaselineMin">The lowest heart rate of the baseline, in beats per minute.</param>
/// <param name="BaselineMax">The highest heart rate of the baseline, in beats per minute.</param>
public sealed record ParticipantBaseline(int MemberId, int BaselineMin, int BaselineMax);
