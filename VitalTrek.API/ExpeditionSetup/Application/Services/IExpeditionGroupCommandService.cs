using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application command service interface for expedition group write operations.</summary>
public interface IExpeditionGroupCommandService
{
    /// <summary>Creates a scheduled expedition group for an enabled route.</summary>
    Task<Result<ExpeditionGroup, ECreateExpeditionGroupError>> Handle(CreateExpeditionGroupCommand command, CancellationToken cancellationToken);

    /// <summary>Assigns the field guide responsible for a group.</summary>
    Task<Result<ExpeditionGroup, EAssignFieldGuideError>> Handle(AssignFieldGuideCommand command, CancellationToken cancellationToken);
}
