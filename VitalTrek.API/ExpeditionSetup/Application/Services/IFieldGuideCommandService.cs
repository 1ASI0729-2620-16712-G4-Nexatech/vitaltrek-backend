using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Application.Patterns;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application command service interface for field guide write operations.</summary>
public interface IFieldGuideCommandService
{
    /// <summary>Registers a field guide profile.</summary>
    Task<Result<FieldGuide, ECreateFieldGuideError>> Handle(CreateFieldGuideCommand command, CancellationToken cancellationToken);
}
