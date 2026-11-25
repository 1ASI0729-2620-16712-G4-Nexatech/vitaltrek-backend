using VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;
using VitalTrek.API.IAM.Interfaces.ACL.Resources;

namespace VitalTrek.API.ExpeditionSetup.Application.Services;

public interface IExpeditionStartService
{
    /// <summary>Validates preparation and stages the group transition in the caller's unit of work.</summary>
    Task<ExpeditionStartStatus> StageStart(AuthenticatedActor actor, int expeditionGroupId,
        DateTimeOffset startedAt, CancellationToken cancellationToken = default);
}
