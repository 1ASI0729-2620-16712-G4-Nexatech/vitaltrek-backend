namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while registering a manifest participant.
/// </summary>
public enum ECreateManifestEntryError
{
    /// <summary>The participant data is invalid.</summary>
    InvalidData,

    /// <summary>The group does not exist or is not assigned to the requester.</summary>
    ExpeditionGroupNotFound,

    /// <summary>The group already started or finished.</summary>
    ExpeditionGroupNotScheduled,

    /// <summary>The identity document is already registered in the group.</summary>
    DuplicateIdentityDocument,

    /// <summary>The identity document is registered in another group with the same departure date.</summary>
    IdentityDocumentDateConflict,

    /// <summary>The group reached its maximum capacity.</summary>
    CapacityExceeded,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
