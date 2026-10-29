using VitalTrek.API.IAM.Domain.Model.Commands;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Interfaces.Rest.Resources;

namespace VitalTrek.API.IAM.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="SignInCommand"/> instances from REST resources.
/// </summary>
public static class SignInCommandFromResourceAssembler
{
    /// <summary>
    ///     Transforms a <see cref="SignInResource"/> into a <see cref="SignInCommand"/>.
    /// </summary>
    /// <param name="resource">The sign-in resource.</param>
    /// <returns>The sign-in command.</returns>
    public static SignInCommand ToCommandFromResource(SignInResource resource) =>
        new(new Username(resource.Username), resource.Password);
}
