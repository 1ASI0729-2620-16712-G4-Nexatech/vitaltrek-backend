namespace VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;

/// <summary>
///     Nature of an incident declared during an expedition.
/// </summary>
public enum IncidentType
{
    /// <summary>A participant requires medical attention.</summary>
    Medical,

    /// <summary>A physical accident occurred during the route.</summary>
    Accident,

    /// <summary>A participant is separated from the group and cannot be located.</summary>
    Missing,

    /// <summary>The environment turned the route unsafe, such as weather or terrain conditions.</summary>
    Environmental
}

/// <summary>
///     Provides conversions between <see cref="IncidentType"/> values and their external codes.
/// </summary>
public static class IncidentTypeExtensions
{
    /// <summary>Converts the type into its external lowercase code.</summary>
    public static string ToCode(this IncidentType type) => type.ToString().ToLowerInvariant();

    /// <summary>Converts an external code into an <see cref="IncidentType"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when the code is unknown.</exception>
    public static IncidentType FromCode(string code) =>
        Enum.TryParse<IncidentType>(code?.Trim(), true, out var type) && Enum.IsDefined(type)
            ? type
            : throw new ArgumentException($"Unknown incident type '{code}'.", nameof(code));
}
