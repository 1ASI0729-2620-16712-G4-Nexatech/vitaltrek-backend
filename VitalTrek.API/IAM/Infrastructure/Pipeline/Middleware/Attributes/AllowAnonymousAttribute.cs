namespace VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Marks an action as accessible without authentication, overriding <see cref="AuthorizeAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AllowAnonymousAttribute : Attribute;
