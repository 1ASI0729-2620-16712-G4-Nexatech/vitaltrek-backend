namespace VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;

/// <summary>
///     Value object with the evidence that supports an evaluated alert: the observed value, the expected limits, the
///     unit and the original moment of the observation. The moment is the capture time of the reading or passage, not
///     the time it was synchronized or evaluated.
/// </summary>
public sealed record AlertEvidence
{
    /// <summary>Unit of heart rate values, in beats per minute.</summary>
    public const string BeatsPerMinute = "bpm";

    /// <summary>Unit of distances, in meters.</summary>
    public const string Meters = "m";

    /// <summary>Unit of elapsed times, in minutes.</summary>
    public const string Minutes = "min";

    /// <summary>Gets the observed value.</summary>
    public decimal ObservedValue { get; }

    /// <summary>Gets the lowest expected value, if the rule has one.</summary>
    public decimal? ExpectedMinimum { get; }

    /// <summary>Gets the highest expected value, if the rule has one.</summary>
    public decimal? ExpectedMaximum { get; }

    /// <summary>Gets the unit of the observed and expected values.</summary>
    public string Unit { get; }

    /// <summary>Gets the original moment of the observation, in UTC.</summary>
    public DateTimeOffset RecordedAt { get; }

    /// <summary>Gets the client identity of the reading or passage that supports the evidence, if any.</summary>
    public Guid? SourceRecordId { get; }

    /// <summary>Gets whether the observed value comes from the academic simulation.</summary>
    public bool Simulated { get; }

    /// <summary>
    ///     Initializes a new evidence.
    /// </summary>
    /// <param name="observedValue">The observed value.</param>
    /// <param name="expectedMinimum">The lowest expected value, if any.</param>
    /// <param name="expectedMaximum">The highest expected value, if any.</param>
    /// <param name="unit">The unit: <see cref="BeatsPerMinute"/>, <see cref="Meters"/> or <see cref="Minutes"/>.</param>
    /// <param name="recordedAt">The original moment of the observation.</param>
    /// <param name="sourceRecordId">The client identity of the supporting record, if any.</param>
    /// <param name="simulated">Whether the observed value comes from the academic simulation.</param>
    /// <exception cref="ArgumentException">Thrown when the evidence data is invalid.</exception>
    public AlertEvidence(decimal observedValue, decimal? expectedMinimum, decimal? expectedMaximum, string unit,
        DateTimeOffset recordedAt, Guid? sourceRecordId = null, bool simulated = false)
    {
        if (expectedMinimum is null && expectedMaximum is null)
            throw new ArgumentException("An evidence requires at least one expected limit.", nameof(expectedMaximum));
        if (expectedMinimum > expectedMaximum)
            throw new ArgumentException("The expected minimum cannot exceed the expected maximum.", nameof(expectedMinimum));
        var normalizedUnit = unit?.Trim();
        if (normalizedUnit is not (BeatsPerMinute or Meters or Minutes))
            throw new ArgumentException($"Unknown evidence unit '{unit}'.", nameof(unit));
        if (sourceRecordId == Guid.Empty)
            throw new ArgumentException("The source record identity cannot be empty.", nameof(sourceRecordId));
        ObservedValue = observedValue;
        ExpectedMinimum = expectedMinimum;
        ExpectedMaximum = expectedMaximum;
        Unit = normalizedUnit;
        RecordedAt = recordedAt.ToUniversalTime();
        SourceRecordId = sourceRecordId;
        Simulated = simulated;
    }
}
