using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Route = VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates.Route;

namespace VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Seeding;

/// <summary>
///     Seeds the demonstration routes, checkpoints, expected time windows, notes and protocols of the demo agency.
///     It runs only when the agency has no routes. Coordinates are fictitious and must not be used for navigation.
/// </summary>
public static class ExpeditionSetupDemoDataSeeder
{
    private const string NoteContent =
        "Demonstration route. Follow the assigned guide. Carry water and warm clothing; check conditions before departure. Coordinates are fictitious and must not be used for real navigation.";

    private const string ProtocolInstructions =
        "Demonstration protocol: verify the alert in the field, document the situation, confirm or dismiss the incident. Operations records emergency coordination when needed. No real messages are sent.";

    private sealed record DemoRoute(
        CreateRouteCommand Command,
        (string Location, double Latitude, double Longitude)[] Checkpoints,
        (int Minimum, int Maximum)[] Windows);

    /// <summary>
    ///     Seeds the demonstration data for the given agency when it has no routes.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="agencyId">The demo agency identifier.</param>
    public static void Seed(AppDbContext context, int agencyId)
    {
        if (agencyId <= 0 || context.Set<Route>().Any(route => route.AgencyId == agencyId)) return;

        DemoRoute[] demoRoutes =
        [
            new(new CreateRouteCommand(agencyId, "Cordillera Huayhuash Circuit", "Llamac, Áncash", "Llamac, Áncash",
                    84.5m, 5240, RouteDifficulty.Advanced, 5760),
                [("Llamac Base Camp", -9.475, -77.475), ("Laguna Mituco", -9.5, -77.45), ("Paso Cacananpunta", -9.525, -77.425),
                 ("Jahuacocha Camp", -9.55, -77.4), ("Llamac Return Point", -9.575, -77.375)],
                [(420, 540), (480, 660), (540, 720), (480, 600)]),
            new(new CreateRouteCommand(agencyId, "Santa Cruz Trek", "Vaquería, Áncash", "Cashapampa, Áncash",
                    50m, 3350, RouteDifficulty.Advanced, 2400),
                [("Vaquería Trailhead", -9.525, -77.475), ("Punta Unión Pass", -9.55, -77.45), ("Llanganuco Camp", -9.575, -77.425),
                 ("Cashapampa Exit", -9.6, -77.4)],
                [(420, 540), (360, 480), (360, 480)]),
            new(new CreateRouteCommand(agencyId, "Marcahuasi Stone Forest Route", "San Pedro de Casta, Lima",
                    "Monumento de la Humanidad, Lima", 14.2m, 780, RouteDifficulty.Intermediate, 480),
                [("San Pedro de Casta", -9.575, -77.475), ("Marcahuasi Amphitheater", -9.6, -77.45),
                 ("Monument of Humanity", -9.625, -77.425)],
                [(180, 240), (120, 180)]),
            new(new CreateRouteCommand(agencyId, "Ausangate Rainbow Circuit", "Tinki, Cusco", "Vinicunca Viewpoint, Cusco",
                    22.6m, 1450, RouteDifficulty.Expert, 960),
                [], [])
        ];

        var bluetoothSequence = 1;
        foreach (var demoRoute in demoRoutes)
        {
            var route = new Route(demoRoute.Command);
            context.Set<Route>().Add(route);
            context.SaveChanges();

            var checkpoints = demoRoute.Checkpoints.Select((checkpoint, index) => new Checkpoint(
                new CreateCheckpointCommand(agencyId, route.Id, checkpoint.Location, index + 1,
                    $"VT-CP-{bluetoothSequence++:D3}", checkpoint.Latitude, checkpoint.Longitude))).ToList();
            context.Set<Checkpoint>().AddRange(checkpoints);
            context.SaveChanges();

            for (var index = 0; index < demoRoute.Windows.Length; index++)
                context.Set<ExpectedTimeWindow>().Add(new ExpectedTimeWindow(new CreateExpectedTimeWindowCommand(agencyId,
                    route.Id, checkpoints[index].Id, checkpoints[index + 1].Id, demoRoute.Windows[index].Minimum,
                    demoRoute.Windows[index].Maximum)));

            context.Set<RouteNote>().Add(new RouteNote(new CreateRouteNoteCommand(agencyId, route.Id, route.Name, NoteContent, 1)));
            context.Set<SafetyProtocol>().Add(new SafetyProtocol(new CreateSafetyProtocolCommand(agencyId, route.Id,
                ProtocolInstructions)));
            if (checkpoints.Count > 0) route.Enable(checkpoints.Count);
            context.SaveChanges();
        }
    }
}
