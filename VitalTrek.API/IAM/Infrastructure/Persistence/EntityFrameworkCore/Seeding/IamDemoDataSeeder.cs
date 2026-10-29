using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.Commands;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace VitalTrek.API.IAM.Infrastructure.Persistence.EntityFrameworkCore.Seeding;

/// <summary>
///     Seeds the demonstration agencies and accounts used by the web applications.
///     It runs only when the database has no agencies, so it never overwrites real data.
/// </summary>
public static class IamDemoDataSeeder
{
    /// <summary>
    ///     Password shared by every demonstration account.
    /// </summary>
    public const string DemoPassword = "VitalTrek123!";

    /// <summary>
    ///     Seeds the demonstration data when the database is empty.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="hashingService">The password hashing service.</param>
    public static void Seed(AppDbContext context, IHashingService hashingService)
    {
        if (context.Set<Agency>().Any()) return;

        var demoAgency = new Agency(new SignUpCommand("VitalTrek Adventure Agency", "Operations Administrator",
            new Username("admin@vitaltrek.demo"), DemoPassword));
        var otherAgency = new Agency(new SignUpCommand("Andes Other Agency", "Other Administrator",
            new Username("other@vitaltrek.demo"), DemoPassword));
        context.Set<Agency>().AddRange(demoAgency, otherAgency);
        context.SaveChanges();

        var passwordHash = hashingService.HashPassword(DemoPassword);
        context.Set<User>().AddRange(
            new User(new Username("admin@vitaltrek.demo"), passwordHash, "Operations Administrator",
                Role.OperationsAdministrator, demoAgency.Id),
            new User(new Username("guide@vitaltrek.demo"), passwordHash, "Elena Vargas",
                Role.FieldGuide, demoAgency.Id, fieldGuideId: 1),
            new User(new Username("guide2@vitaltrek.demo"), passwordHash, "Diego Quispe",
                Role.FieldGuide, demoAgency.Id, fieldGuideId: 2),
            new User(new Username("tourist@vitaltrek.demo"), passwordHash, "Valeria Torres",
                Role.AdventureTourist, demoAgency.Id, identityDocument: "70010001"),
            new User(new Username("other@vitaltrek.demo"), passwordHash, "Other Administrator",
                Role.OperationsAdministrator, otherAgency.Id));
        context.SaveChanges();
    }
}
