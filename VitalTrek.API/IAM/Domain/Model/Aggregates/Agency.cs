using VitalTrek.API.IAM.Domain.Model.Commands;

namespace VitalTrek.API.IAM.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing a tour operator agency that subscribes to VitalTrek.
/// </summary>
public partial class Agency
{
    private const int MaxNameLength = 150;

    /// <summary>
    ///     Gets the unique identifier of the agency.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    ///     Gets the commercial name of the agency.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Agency"/> class for Entity Framework Core.
    /// </summary>
    public Agency()
    {
        Name = string.Empty;
    }

    /// <summary>
    ///     Initializes a new agency from a sign-up command.
    /// </summary>
    /// <param name="command">The sign-up command containing the agency name.</param>
    /// <exception cref="ArgumentException">Thrown when the agency name is empty or too long.</exception>
    public Agency(SignUpCommand command)
    {
        var name = command.AgencyName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Agency name cannot be empty.", nameof(command));
        if (name.Length > MaxNameLength)
            throw new ArgumentException($"Agency name cannot exceed {MaxNameLength} characters.", nameof(command));
        Name = name;
    }
}
