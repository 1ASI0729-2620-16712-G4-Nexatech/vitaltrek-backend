namespace VitalTrek.API.IAM.Interfaces.ACL.Resources;

/// <summary>
///     Immutable IAM contract describing the authenticated requester without exposing the user entity or credentials.
///     This contract is assembled from the server-resolved user, never from request input.
/// </summary>
public sealed record AuthenticatedActor(int UserId, int AgencyId, string RoleCode,
    int? FieldGuideId = null, string? IdentityDocument = null);
