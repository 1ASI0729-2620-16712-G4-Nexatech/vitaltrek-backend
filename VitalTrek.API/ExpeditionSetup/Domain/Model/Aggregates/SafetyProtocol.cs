using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing the procedures defined by the agency to prevent, attend and escalate incidents
///     on a route. A route has a single safety protocol.
/// </summary>
public partial class SafetyProtocol
{
    /// <summary>Gets the protocol identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that owns the protocol.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the route the protocol applies to.</summary>
    public int RouteId { get; private set; }

    /// <summary>Gets the protocol instructions.</summary>
    public string Instructions { get; private set; }

    /// <summary>Gets the protocol status.</summary>
    public SafetyProtocolStatus Status { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SafetyProtocol"/> class for Entity Framework Core.
    /// </summary>
    public SafetyProtocol()
    {
        Instructions = string.Empty;
    }

    /// <summary>
    ///     Initializes a new active safety protocol.
    /// </summary>
    /// <param name="command">The create safety protocol command.</param>
    /// <exception cref="ArgumentException">Thrown when the protocol data is invalid.</exception>
    public SafetyProtocol(CreateSafetyProtocolCommand command)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        RouteId = DomainGuard.RequirePositive(command.RouteId, nameof(command.RouteId));
        Instructions = DomainGuard.RequireText(command.Instructions, 4000, nameof(command.Instructions));
        Status = SafetyProtocolStatus.Active;
    }

    /// <summary>
    ///     Updates the protocol instructions and status.
    /// </summary>
    /// <param name="command">The update safety protocol command.</param>
    /// <exception cref="ArgumentException">Thrown when the protocol data is invalid.</exception>
    public void Update(UpdateSafetyProtocolCommand command)
    {
        Instructions = DomainGuard.RequireText(command.Instructions, 4000, nameof(command.Instructions));
        Status = command.Status;
    }
}
