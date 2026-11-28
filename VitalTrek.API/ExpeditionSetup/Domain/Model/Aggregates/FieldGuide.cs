using System.Text.RegularExpressions;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing the operational profile of an accredited guide of an agency. The operational code
///     is unique in the agency and tells apart guides with the same name without exposing personal data.
/// </summary>
public partial class FieldGuide
{
    /// <summary>Maximum length of the operational code.</summary>
    public const int MaximumCodeLength = 20;

    /// <summary>Gets the field guide identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency the guide works for.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the full name of the guide.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the operational code of the guide, unique in the agency, such as GU-001.</summary>
    public string Code { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FieldGuide"/> class for Entity Framework Core.
    /// </summary>
    public FieldGuide()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    /// <summary>
    ///     Initializes a new field guide profile. When the command has no code a random operational code is assigned;
    ///     the command service normally provides the next sequential code of the agency.
    /// </summary>
    /// <param name="command">The create field guide command.</param>
    /// <exception cref="ArgumentException">Thrown when the data is invalid.</exception>
    public FieldGuide(CreateFieldGuideCommand command)
    {
        AgencyId = DomainGuard.RequirePositive(command.AgencyId, nameof(command.AgencyId));
        Name = DomainGuard.RequireText(command.Name, 120, nameof(command.Name));
        Code = string.IsNullOrWhiteSpace(command.Code)
            ? $"GU-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
            : NormalizeCode(command.Code);
    }

    /// <summary>Normalizes an operational code to upper case and validates its format.</summary>
    /// <exception cref="ArgumentException">Thrown when the code has an invalid format.</exception>
    public static string NormalizeCode(string code)
    {
        var normalized = DomainGuard.RequireText(code, MaximumCodeLength, nameof(code)).ToUpperInvariant();
        if (!CodeRegex().IsMatch(normalized))
            throw new ArgumentException("Code must have 2 to 20 letters, digits or hyphens.", nameof(code));
        return normalized;
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]{1,19}$")]
    private static partial Regex CodeRegex();
}
