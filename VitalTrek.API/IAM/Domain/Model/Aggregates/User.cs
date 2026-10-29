using VitalTrek.API.IAM.Domain.Model.ValueObjects;

namespace VitalTrek.API.IAM.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing a user account that belongs to a single agency.
/// </summary>
public partial class User
{
    private const int MaxNameLength = 120;
    private const int MaxIdentityDocumentLength = 20;

    /// <summary>
    ///     Gets the unique identifier of the user.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    ///     Gets the e-mail address used to sign in.
    /// </summary>
    public Username Username { get; private set; }

    /// <summary>
    ///     Gets the salted hash of the user's password. The plain password is never stored.
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    ///     Gets the full name of the user.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    ///     Gets the role that determines the operations available to the user.
    /// </summary>
    public Role Role { get; private set; }

    /// <summary>
    ///     Gets the identifier of the agency the user belongs to.
    /// </summary>
    public int AgencyId { get; private set; }

    /// <summary>
    ///     Gets the field guide profile linked to the account. Only present for <see cref="Role.FieldGuide"/> accounts.
    /// </summary>
    public int? FieldGuideId { get; private set; }

    /// <summary>
    ///     Gets the identity document registered in the tourist manifest. Only present for <see cref="Role.AdventureTourist"/> accounts.
    /// </summary>
    public string? IdentityDocument { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="User"/> class for Entity Framework Core.
    /// </summary>
    public User()
    {
        Username = null!;
        PasswordHash = string.Empty;
        Name = string.Empty;
    }

    /// <summary>
    ///     Initializes a new user account.
    /// </summary>
    /// <param name="username">The validated username.</param>
    /// <param name="passwordHash">The hashed password.</param>
    /// <param name="name">The full name of the user.</param>
    /// <param name="role">The role of the user.</param>
    /// <param name="agencyId">The identifier of the agency the user belongs to.</param>
    /// <param name="fieldGuideId">The linked field guide profile, required for field guides.</param>
    /// <param name="identityDocument">The linked identity document, required for adventure tourists.</param>
    /// <exception cref="ArgumentException">Thrown when the account data violates the account invariants.</exception>
    public User(Username username, string passwordHash, string name, Role role, int agencyId,
        int? fieldGuideId = null, string? identityDocument = null)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName) || normalizedName.Length > MaxNameLength)
            throw new ArgumentException($"Name is required and cannot exceed {MaxNameLength} characters.", nameof(name));
        if (agencyId <= 0)
            throw new ArgumentException("Agency identifier must be positive.", nameof(agencyId));
        if (role == Role.FieldGuide && fieldGuideId is not > 0)
            throw new ArgumentException("Field guide accounts require a field guide identifier.", nameof(fieldGuideId));
        var normalizedDocument = identityDocument?.Trim();
        if (role == Role.AdventureTourist &&
            (string.IsNullOrWhiteSpace(normalizedDocument) || normalizedDocument.Length > MaxIdentityDocumentLength))
            throw new ArgumentException("Adventure tourist accounts require a valid identity document.", nameof(identityDocument));

        Username = username;
        PasswordHash = passwordHash;
        Name = normalizedName;
        Role = role;
        AgencyId = agencyId;
        FieldGuideId = role == Role.FieldGuide ? fieldGuideId : null;
        IdentityDocument = role == Role.AdventureTourist ? normalizedDocument : null;
    }

    /// <summary>
    ///     Indicates whether the user can manage the resources of the given agency.
    /// </summary>
    /// <param name="agencyId">The agency identifier to check.</param>
    /// <returns><see langword="true"/> when the user is an administrator of that agency.</returns>
    public bool IsAdministratorOf(int agencyId) => Role == Role.OperationsAdministrator && AgencyId == agencyId;
}
