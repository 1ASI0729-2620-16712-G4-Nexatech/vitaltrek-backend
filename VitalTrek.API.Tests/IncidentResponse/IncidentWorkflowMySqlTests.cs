using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Domain.Services;
using VitalTrek.API.IncidentResponse.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using VitalTrek.API.Tests.FieldTracking;
using Xunit;

namespace VitalTrek.API.Tests.IncidentResponse;

/// <summary>
///     Opt-in tests against disposable local MySQL 8 databases (VITALTREK_TEST_MYSQL): the IncidentWorkflowIntegration
///     migration upgrades a demonstration database, the unique indexes and concurrency tokens hold under real
///     transactions, and the preparation scope protects the health information.
/// </summary>
public sealed class IncidentWorkflowMySqlTests
{
    private const string PreviousMigration = "20261127171639_AddSafetyAlertEvidence";

    [LocalMySqlFact]
    public async Task UpgradeKeepsLegacyRowsReadableAndMatchesTheRuntimeModel()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync(migration: PreviousMigration);
        await using (var connection = new MySqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var legacy = new MySqlCommand("""
                INSERT INTO tracking_sessions (agency_id, expedition_group_id, started_at, status)
                VALUES (1, 101, UTC_TIMESTAMP(), 'active');
                INSERT INTO incidents (session_id, agency_id, reported_at, incident_type, severity, description, status)
                VALUES (LAST_INSERT_ID(), 1, UTC_TIMESTAMP(), 'medical', 'high', 'Legacy incident.', 'inresponse');
                INSERT INTO incident_notifications (incident_id, agency_id, recipient_type, sent_at, channel, status)
                VALUES (LAST_INSERT_ID(), 1, 'emergency_contact', UTC_TIMESTAMP(), 'sms', 'sent'),
                       (LAST_INSERT_ID(), 1, 'rescue_entity', UTC_TIMESTAMP(), 'satellite_radio', 'sent');
                """, connection);
            await legacy.ExecuteNonQueryAsync();
        }

        await using var context = database.Open();
        await context.Database.MigrateAsync();

        Assert.False(context.Database.HasPendingModelChanges());
        var incident = await context.Set<Incident>().SingleAsync();
        Assert.Equal(IncidentStatus.Confirmed, incident.Status);
        Assert.Equal(101, incident.ExpeditionGroupId);
        var notifications = await context.Set<IncidentNotification>().ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, notification =>
        {
            Assert.Equal(NotificationStatus.Failed, notification.Status);
            Assert.Equal("legacy_unverified", notification.FailureCode);
            Assert.NotEqual(Guid.Empty, notification.NotificationId);
        });
        Assert.NotEqual(notifications[0].NotificationId, notifications[1].NotificationId);
        Assert.Equal("GU-010", (await context.Set<FieldGuide>().SingleAsync()).Code);
    }

    [LocalMySqlFact]
    public async Task UniqueIndexesKeepOneEvacuationActionPerClientIdentityAndOneIncidentPerAlert()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using var context = database.Open();
        var incident = Emergency();
        context.Add(incident);
        await context.SaveChangesAsync();
        var command = new RecordEvacuationActionCommand(1, incident.Id, Guid.NewGuid(), "Descent started.", null,
            EvacuationActionStatus.InProgress, DateTimeOffset.UtcNow);
        var fingerprint = IncidentContentFingerprint.ForEvacuationAction(command);
        context.Add(new EvacuationAction(command, 101, 2, DateTimeOffset.UtcNow, fingerprint));
        await context.SaveChangesAsync();

        await using var concurrent = database.Open();
        concurrent.Add(new EvacuationAction(command, 101, 3, DateTimeOffset.UtcNow, fingerprint));
        await Assert.ThrowsAsync<DbUpdateException>(() => concurrent.SaveChangesAsync());

        await using var alerts = database.Open();
        alerts.Add(Reported(alertId: 77));
        await alerts.SaveChangesAsync();
        alerts.Add(Reported(alertId: 77));
        await Assert.ThrowsAsync<DbUpdateException>(() => alerts.SaveChangesAsync());
        alerts.ChangeTracker.Clear();
        alerts.Add(Reported());
        alerts.Add(Reported());
        await alerts.SaveChangesAsync();

        await using var persisted = database.Open();
        var action = await new EvacuationActionRepository(persisted).FindByClientActionIdAsync(1, command.ClientActionId);
        Assert.Equal(2, action!.RecordedByUserId);
        Assert.Equal(1, await persisted.Set<EvacuationAction>().CountAsync());
    }

    [LocalMySqlFact]
    public async Task ConcurrentAttemptsOfTheSameNotificationCannotBothStart()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using (var context = database.Open())
        {
            var incident = Emergency();
            context.Add(incident);
            await context.SaveChangesAsync();
            context.Add(new IncidentNotification(new SendNotificationCommand(1, incident.Id, Guid.NewGuid(),
                    NotificationRecipientType.AgencyStaff, "team-test@vitaltrek.test"), 1, "[VitalTrek exercise] s", "b",
                new string('a', 64), DateTimeOffset.UtcNow));
            await context.SaveChangesAsync();
        }

        await using var first = database.Open();
        await using var second = database.Open();
        var lease = TimeSpan.FromSeconds(90);
        var a = await first.Set<IncidentNotification>().SingleAsync();
        var b = await second.Set<IncidentNotification>().SingleAsync();
        a.StartAttempt(DateTimeOffset.UtcNow, lease);
        b.StartAttempt(DateTimeOffset.UtcNow, lease);
        await first.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await using var persisted = database.Open();
        Assert.Equal(1, (await persisted.Set<IncidentNotification>().SingleAsync()).AttemptCount);
    }

    [LocalMySqlFact]
    public async Task StatusIsAConcurrencyTokenSoConfirmAndDismissCannotBothCommit()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        int id;
        await using (var context = database.Open())
        {
            var incident = Reported();
            context.Add(incident);
            await context.SaveChangesAsync();
            id = incident.Id;
        }

        await using var guide = database.Open();
        await using var other = database.Open();
        var confirming = await new IncidentRepository(guide).FindByIdAndAgencyIdAsync(id, 1);
        var dismissing = await new IncidentRepository(other).FindByIdAndAgencyIdAsync(id, 1);
        confirming!.Confirm(2, DateTimeOffset.UtcNow);
        dismissing!.Dismiss(2, DateTimeOffset.UtcNow);
        await guide.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => other.SaveChangesAsync());
        await using var persisted = database.Open();
        var stored = await new IncidentRepository(persisted).FindByIdAndAgencyIdAsync(id, 1);
        Assert.Equal(IncidentStatus.Confirmed, stored!.Status);
        Assert.Equal(2, stored.StatusChanges.Count);
    }

    [LocalMySqlFact]
    public async Task PreparationScopeProtectsHealthInformationAndContacts()
    {
        await using var database = await LocalTrackingDatabase.CreateAsync();
        await using (var context = database.Open())
        {
            var own = await context.Set<ParticipantPreparation>().SingleAsync(preparation => preparation.Id == 80);
            own.RegisterHealthInformation("Allergic to penicillin.", DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

        await using var reader = database.Open();
        var repository = new ParticipantPreparationRepository(reader);
        var tourist = (await repository.FindByScopeAsync(new AccessScope(1, IdentityDocument: "DOC-0"), null)).ToArray();
        var assignedGuide = await repository.FindByScopeAsync(new AccessScope(1, 10), 101);
        var otherGuide = await repository.FindByScopeAsync(new AccessScope(1, 99), 101);
        var otherAgency = await repository.FindByScopeAsync(new AccessScope(2), null);

        var visible = Assert.Single(tourist);
        Assert.Equal(80, visible.Id);
        Assert.Equal("Allergic to penicillin.", visible.HealthNotes);
        Assert.Null(await repository.FindByIdAndScopeAsync(81, new AccessScope(1, IdentityDocument: "DOC-0")));
        Assert.Equal(2, assignedGuide.Count());
        Assert.Empty(otherGuide);
        Assert.Empty(otherAgency);
    }

    private static Incident Reported(int? alertId = null) =>
        new(new DeclareIncidentCommand(1, 11, IncidentType.Accident, IncidentSeverity.High, "Fall.",
            DateTimeOffset.UtcNow.AddMinutes(-5), alertId), 101, 2, DateTimeOffset.UtcNow);

    private static Incident Emergency()
    {
        var incident = Reported();
        incident.Confirm(2, DateTimeOffset.UtcNow.AddMinutes(-4));
        incident.DeclareEmergency(1, DateTimeOffset.UtcNow.AddMinutes(-3));
        return incident;
    }
}
