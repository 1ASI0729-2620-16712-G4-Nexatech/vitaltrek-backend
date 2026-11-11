using VitalTrek.API.SafetyMonitoring.Domain.Model.Commands;
using VitalTrek.API.SafetyMonitoring.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.SafetyMonitoring.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing an early warning alert raised when a risk condition is detected during a
///     tracking session.
/// </summary>
public partial class EarlyWarningAlert
{
    /// <summary>Maximum length of the alert description.</summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>Gets the alert identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the tracking session the alert belongs to.</summary>
    public int SessionId { get; private set; }

    /// <summary>Gets the expedition group tracked by the session.</summary>
    public int ExpeditionGroupId { get; private set; }

    /// <summary>Gets the agency that owns the alert.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the checkpoint related to the alert, if any.</summary>
    public int? CheckpointId { get; private set; }

    /// <summary>Gets the type of risk condition.</summary>
    public AlertType AlertType { get; private set; }

    /// <summary>Gets the severity of the alert.</summary>
    public AlertSeverity Severity { get; private set; }

    /// <summary>Gets the moment the alert was raised.</summary>
    public DateTimeOffset RaisedAt { get; private set; }

    /// <summary>Gets the lifecycle status of the alert.</summary>
    public AlertStatus Status { get; private set; }

    /// <summary>Gets the description of the risk condition.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EarlyWarningAlert"/> class for Entity Framework Core.
    /// </summary>
    public EarlyWarningAlert()
    {
    }

    /// <summary>
    ///     Initializes a new active early warning alert.
    /// </summary>
    /// <param name="command">The raise early warning alert command.</param>
    /// <param name="expeditionGroupId">The expedition group tracked by the session.</param>
    /// <param name="raisedAt">The moment the alert is raised.</param>
    /// <exception cref="ArgumentException">Thrown when the alert data is invalid.</exception>
    public EarlyWarningAlert(RaiseEarlyWarningAlertCommand command, int expeditionGroupId, DateTimeOffset raisedAt)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        SessionId = DomainGuard.RequirePositive(command.SessionId, nameof(command.SessionId));
        ExpeditionGroupId = DomainGuard.RequirePositive(expeditionGroupId, nameof(expeditionGroupId));
        if (command.CheckpointId is not null)
            CheckpointId = DomainGuard.RequirePositive(command.CheckpointId.Value, nameof(command.CheckpointId));
        if (!Enum.IsDefined(command.AlertType))
            throw new ArgumentException("Unknown alert type.", nameof(command));
        if (!Enum.IsDefined(command.Severity))
            throw new ArgumentException("Unknown alert severity.", nameof(command));
        AlertType = command.AlertType;
        Severity = command.Severity;
        Description = DomainGuard.RequireText(command.Description, DescriptionMaxLength, nameof(command.Description));
        RaisedAt = raisedAt;
        Status = AlertStatus.Active;
    }

    /// <summary>Gets whether the alert still requires attention (active or acknowledged).</summary>
    public bool IsOpen => Status is AlertStatus.Active or AlertStatus.Acknowledged;

    /// <summary>Gets whether the alert is critical and has not been dismissed.</summary>
    public bool IsCritical => Severity == AlertSeverity.Critical && Status != AlertStatus.Dismissed;

    /// <summary>Acknowledges an active alert.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the alert is not active.</exception>
    public void Acknowledge()
    {
        if (Status != AlertStatus.Active)
            throw new InvalidOperationException("Only an active alert can be acknowledged.");
        Status = AlertStatus.Acknowledged;
    }

    /// <summary>Dismisses an open alert as a false alarm.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the alert is already closed.</exception>
    public void Dismiss()
    {
        if (!IsOpen)
            throw new InvalidOperationException("Only an open alert can be dismissed.");
        Status = AlertStatus.Dismissed;
    }

    /// <summary>Resolves an acknowledged alert.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the alert was not acknowledged.</exception>
    public void Resolve()
    {
        if (Status != AlertStatus.Acknowledged)
            throw new InvalidOperationException("Only an acknowledged alert can be resolved.");
        Status = AlertStatus.Resolved;
    }

    /// <summary>
    ///     Determines the severity of a delay: high once the maximum expected time is exceeded and critical once
    ///     the elapsed time doubles it.
    /// </summary>
    /// <param name="elapsed">The time elapsed since the group passed the origin checkpoint.</param>
    /// <param name="maximumMinutes">The maximum expected minutes of the segment.</param>
    /// <returns>The delay severity, or <see langword="null"/> when the segment is still within its window.</returns>
    public static AlertSeverity? DelaySeverityFor(TimeSpan elapsed, int maximumMinutes)
    {
        if (maximumMinutes <= 0)
            throw new ArgumentException("Maximum minutes must be greater than zero.", nameof(maximumMinutes));
        var maximum = TimeSpan.FromMinutes(maximumMinutes);
        if (elapsed <= maximum) return null;
        return elapsed >= maximum * 2 ? AlertSeverity.Critical : AlertSeverity.High;
    }
}
