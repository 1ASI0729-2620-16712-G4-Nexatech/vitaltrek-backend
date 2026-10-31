using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.Shared.Domain.Model;
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

    /// <summary>
    ///     Seeds the demonstration field guides, wearables, expedition groups, manifests and preparations of the demo
    ///     agency when it has no field guides. Departure dates are relative to the current date so the groups stay valid.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="agencyId">The demo agency identifier.</param>
    public static void SeedExpeditionGroups(AppDbContext context, int agencyId)
    {
        if (agencyId <= 0 || context.Set<FieldGuide>().Any(guide => guide.AgencyId == agencyId)) return;
        var routes = context.Set<Route>().Where(route => route.AgencyId == agencyId).ToDictionary(route => route.Name, route => route.Id);
        if (!routes.ContainsKey("Cordillera Huayhuash Circuit") || !routes.ContainsKey("Santa Cruz Trek") ||
            !routes.ContainsKey("Marcahuasi Stone Forest Route")) return;

        var guides = new[] { "Elena Vargas", "Diego Huamán", "Lucía Paredes", "Mateo Salazar" }
            .Select(name => new FieldGuide(new CreateFieldGuideCommand(agencyId, name))).ToList();
        context.Set<FieldGuide>().AddRange(guides);
        var wearables = Enumerable.Range(1, 20)
            .Select(number => new WearableDevice(new CreateWearableDeviceCommand(agencyId, $"VT-W-{number:D3}"))).ToList();
        context.Set<WearableDevice>().AddRange(wearables);
        context.SaveChanges();

        var today = AgencyCalendar.Today();
        (string Name, string Route, int DaysFromToday, int Capacity, FieldGuide? Guide)[] demoGroups =
        [
            ("Huayhuash Summit Team", "Cordillera Huayhuash Circuit", 1, 12, guides[0]),
            ("Santa Cruz Glacier Team", "Santa Cruz Trek", 0, 10, guides[1]),
            ("Marcahuasi Weekend Group", "Marcahuasi Stone Forest Route", 5, 14, guides[2]),
            ("Huayhuash Acclimatization Group", "Cordillera Huayhuash Circuit", 6, 8, guides[3]),
            ("Santa Cruz Orientation Group", "Santa Cruz Trek", 7, 10, null)
        ];
        var groups = demoGroups.Select(demo =>
        {
            var group = new ExpeditionGroup(new CreateExpeditionGroupCommand(agencyId, routes[demo.Route], demo.Name,
                today.AddDays(demo.DaysFromToday), demo.Capacity), today);
            if (demo.Guide is not null) group.AssignFieldGuide(demo.Guide.Id);
            return group;
        }).ToList();
        context.Set<ExpeditionGroup>().AddRange(groups);
        context.SaveChanges();

        (int Group, string FullName, string Document)[] demoParticipants =
        [
            (0, "Valeria Torres", "70010001"), (0, "Andrés Medina", "70010002"), (0, "Camila Rojas", "70010003"),
            (0, "Sofía Herrera", "70010004"), (0, "Bruno Castillo", "70010005"),
            (1, "Mariana Flores", "70020001"), (1, "Joaquín Ramos", "70020002"), (1, "Renata Silva", "70020003"),
            (1, "Pablo Núñez", "70020004"),
            (2, "Daniela Cruz", "70030001"), (2, "Martín Vega", "70030002"), (2, "Ariana Castro", "70030003"),
            (3, "Gabriel Soto", "70040001"), (3, "Elisa Navarro", "70040002")
        ];
        var entries = demoParticipants.Select(participant => new ManifestEntry(new CreateManifestEntryCommand(agencyId,
            groups[participant.Group].Id, participant.FullName, participant.Document, null))).ToList();
        context.Set<ManifestEntry>().AddRange(entries);
        context.SaveChanges();

        var now = DateTimeOffset.UtcNow;
        var demoContact = new EmergencyContact("Demo Emergency Contact", "+51 900 000 000", "Family");
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var preparation = new ParticipantPreparation(agencyId, entry.ExpeditionGroupId, entry.Id);
            // The first nine participants (groups 1 and 2) already have a wearable; all of them except the demo
            // tourist account (Valeria Torres) completed their consent.
            if (index < 9)
            {
                wearables[index].MarkAssigned();
                preparation.AssignWearable(wearables[index].Id);
                if (index > 0)
                {
                    preparation.RegisterEmergencyContact(demoContact);
                    preparation.GrantConsent(now);
                }
            }
            context.Set<ParticipantPreparation>().Add(preparation);
        }

        groups[1].Start(now);
        context.SaveChanges();
    }
}
