namespace VitalTrek.API.ExpeditionSetup.Application.Errors;

/// <summary>
///     Error states that may occur while registering a field guide.
/// </summary>
public enum ECreateFieldGuideError
{
    /// <summary>The field guide data is invalid.</summary>
    InvalidData,

    /// <summary>An unexpected persistence or system error occurred.</summary>
    UnexpectedError
}
