using VitalTrek.API.IAM.Interfaces.ACL.Resources;
using VitalTrek.API.IncidentResponse.Application.Errors;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.IncidentResponse.Application.Services;

/// <summary>Result of recording an evacuation action: the persisted action and whether this request created it.</summary>
/// <param name="Action">The persisted action.</param>
/// <param name="Created">False when the request was a retry of an action already persisted.</param>
public sealed record EvacuationActionRecordResult(EvacuationAction Action, bool Created);

/// <summary>
///     Application command service interface for evacuation actions captured by the assigned field guide.
/// </summary>
public interface IEvacuationActionCommandService
{
    /// <summary>Records an evacuation action; a retry with the same identity and content returns the first record.</summary>
    Task<Result<EvacuationActionRecordResult, ERecordEvacuationActionError>> Handle(
        RecordEvacuationActionCommand command, AuthenticatedActor actor, CancellationToken cancellationToken);
}
