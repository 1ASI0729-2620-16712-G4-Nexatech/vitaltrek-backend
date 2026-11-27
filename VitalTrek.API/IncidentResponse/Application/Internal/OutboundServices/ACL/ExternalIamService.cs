using VitalTrek.API.IAM.Interfaces.ACL;

namespace VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.ACL;

/// <summary>
///     Outbound service that validates notification recipients through the IAM facade.
/// </summary>
/// <param name="iamContextFacade">The IAM facade.</param>
public class ExternalIamService(IIamContextFacade iamContextFacade)
{
    /// <summary>Checks whether an e-mail address belongs to a staff account of the agency.</summary>
    public async Task<bool> IsAgencyStaffEmail(int agencyId, string email, CancellationToken cancellationToken) =>
        await iamContextFacade.IsAgencyStaffEmail(agencyId, email, cancellationToken);
}
