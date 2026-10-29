using VitalTrek.API.IAM.Domain.Model.Commands;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Interfaces.Rest.Resources;

namespace VitalTrek.API.IAM.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="SignUpCommand"/> instances from REST resources.
/// </summary>
public static class SignUpCommandFromResourceAssembler
{
    /// <summary>
    ///     Transforms a <see cref="SignUpResource"/> into a <see cref="SignUpCommand"/>.
    /// </summary>
    /// <param name="resource">The sign-up resource.</param>
    /// <returns>The sign-up command.</returns>
    public static SignUpCommand ToCommandFromResource(SignUpResource resource) =>
        new(resource.AgencyName, resource.Name, new Username(resource.Username), resource.Password);
}
