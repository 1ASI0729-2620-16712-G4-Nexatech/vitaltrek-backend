namespace VitalTrek.API.IAM.Domain.Model.ValueObjects;

/// <summary>
///     Roles that a VitalTrek user account can hold.
/// </summary>
public enum Role
{
    /// <summary>
    ///     Agency staff member who configures routes and supervises expeditions from the operations base.
    /// </summary>
    OperationsAdministrator,

    /// <summary>
    ///     Accredited guide who accompanies the expedition group in the field.
    /// </summary>
    FieldGuide,

    /// <summary>
    ///     Tourist who participates in an expedition and consults its information.
    /// </summary>
    AdventureTourist
}

/// <summary>
///     Provides conversions between <see cref="Role"/> values and their external snake_case codes.
/// </summary>
public static class RoleExtensions
{
    private static readonly Dictionary<Role, string> Codes = new()
    {
        [Role.OperationsAdministrator] = "operations_administrator",
        [Role.FieldGuide] = "field_guide",
        [Role.AdventureTourist] = "adventure_tourist"
    };

    /// <summary>
    ///     Converts the role into its external snake_case code.
    /// </summary>
    /// <param name="role">The role to convert.</param>
    /// <returns>The external code of the role.</returns>
    public static string ToCode(this Role role) => Codes[role];

    /// <summary>
    ///     Converts an external snake_case code into a <see cref="Role"/>.
    /// </summary>
    /// <param name="code">The external role code.</param>
    /// <returns>The matching role.</returns>
    /// <exception cref="ArgumentException">Thrown when the code does not match a known role.</exception>
    public static Role FromCode(string code)
    {
        foreach (var (role, value) in Codes)
            if (string.Equals(value, code?.Trim(), StringComparison.OrdinalIgnoreCase)) return role;
        throw new ArgumentException($"Unknown role code '{code}'.", nameof(code));
    }
}
