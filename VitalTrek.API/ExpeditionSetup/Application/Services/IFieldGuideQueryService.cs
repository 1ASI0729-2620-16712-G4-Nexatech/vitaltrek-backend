using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Queries;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

/// <summary>Application query service interface for field guide read operations.</summary>
public interface IFieldGuideQueryService
{
    /// <summary>Retrieves the field guide items visible to the requester.</summary>
    Task<IEnumerable<FieldGuide>> Handle(GetAllFieldGuidesQuery query, CancellationToken cancellationToken);

    /// <summary>Retrieves a field guide item visible to the requester.</summary>
    Task<FieldGuide?> Handle(GetFieldGuideByIdQuery query, CancellationToken cancellationToken);
}
