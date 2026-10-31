using System.Text.RegularExpressions;
using VitalTrek.API.Shared.Domain.Model;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

/// <summary>
///     Value object representing the person to notify when an incident affects an adventure tourist.
/// </summary>
public sealed partial record EmergencyContact
{
    /// <summary>Gets the full name of the contact.</summary>
    public string Name { get; }

    /// <summary>Gets the phone number of the contact.</summary>
    public string Phone { get; }

    /// <summary>Gets the relationship with the tourist, if provided.</summary>
    public string? Relationship { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EmergencyContact"/> value object.
    /// </summary>
    /// <param name="name">The contact name.</param>
    /// <param name="phone">The contact phone, with digits and optional +, spaces or hyphens.</param>
    /// <param name="relationship">The optional relationship.</param>
    /// <exception cref="ArgumentException">Thrown when the name or phone are invalid.</exception>
    public EmergencyContact(string name, string phone, string? relationship = null)
    {
        Name = DomainGuard.RequireText(name, 120, nameof(name));
        var normalizedPhone = DomainGuard.RequireText(phone, 25, nameof(phone));
        if (!PhoneRegex().IsMatch(normalizedPhone))
            throw new ArgumentException("Phone must contain between 6 and 15 digits.", nameof(phone));
        Phone = normalizedPhone;
        Relationship = string.IsNullOrWhiteSpace(relationship) ? null : DomainGuard.RequireText(relationship, 60, nameof(relationship));
    }

    [GeneratedRegex(@"^\+?(?:[\s-]?\d){6,15}$")]
    private static partial Regex PhoneRegex();
}
