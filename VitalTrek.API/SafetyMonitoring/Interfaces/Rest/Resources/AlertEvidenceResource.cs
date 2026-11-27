namespace VitalTrek.API.SafetyMonitoring.Interfaces.Rest.Resources;

/// <summary>
///     Resource with the evidence of an evaluated alert. RecordedAt is the original capture time of the observation,
///     not the moment it was synchronized or evaluated; Simulated identifies values of the academic simulation.
/// </summary>
public record AlertEvidenceResource(
    decimal ObservedValue,
    decimal? ExpectedMinimum,
    decimal? ExpectedMaximum,
    string Unit,
    DateTimeOffset RecordedAt,
    Guid? SourceRecordId,
    bool Simulated);
