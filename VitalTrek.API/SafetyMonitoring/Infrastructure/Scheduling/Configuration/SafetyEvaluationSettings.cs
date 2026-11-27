namespace VitalTrek.API.SafetyMonitoring.Infrastructure.Scheduling.Configuration;

/// <summary>
///     Settings of the periodic safety evaluation, bound from the <c>SafetyMonitoring</c> configuration section.
/// </summary>
public class SafetyEvaluationSettings
{
    /// <summary>Name of the configuration section.</summary>
    public const string SectionName = "SafetyMonitoring";

    /// <summary>Shortest interval accepted, so a misconfiguration cannot saturate the database.</summary>
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets whether the periodic evaluation runs. Enabled by default.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the seconds between two evaluation cycles. Sixty by default.</summary>
    public int EvaluationIntervalSeconds { get; set; } = 60;

    /// <summary>Gets or sets the maximum pending batches and active sessions handled per cycle (1 to 100).</summary>
    public int MaxItemsPerCycle { get; set; } = 100;

    /// <summary>Gets the interval between cycles, never shorter than <see cref="MinimumInterval"/>.</summary>
    public TimeSpan Interval =>
        TimeSpan.FromSeconds(EvaluationIntervalSeconds) < MinimumInterval
            ? MinimumInterval
            : TimeSpan.FromSeconds(EvaluationIntervalSeconds);

    /// <summary>Gets the items handled per cycle, within the limit of the internal lookups.</summary>
    public int ItemsPerCycle => Math.Clamp(MaxItemsPerCycle, 1, 100);
}
