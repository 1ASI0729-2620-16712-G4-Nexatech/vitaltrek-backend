using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.Notifications;
using Xunit;
using static VitalTrek.API.Tests.IncidentResponse.IncidentWorkflowData;

namespace VitalTrek.API.Tests.IncidentResponse;

/// <summary>
///     I T4 and the e-mail part of I T5 through the real HTTP pipeline, with a test double of the provider: a rejected or
///     unconfigured provider never produces delivered, a retry after a lost response keeps the notification identity and
///     the provider idempotency key, and the message never contains health or personal data.
/// </summary>
public sealed class IncidentNotificationHttpTests(IncidentApiFixture fixture) : IClassFixture<IncidentApiFixture>
{
    [Fact]
    public async Task ProviderWithoutCredentialsLeavesThePersistedNotificationPending()
    {
        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.NotConfigured;
        var incidentId = await fixture.ReportEmergency();
        var notificationId = Guid.NewGuid();

        var (status, body) = await Request(incidentId, notificationId);

        Assert.Equal(201, status);
        Assert.Equal("pending", body.GetProperty("status").GetString());
        Assert.Equal("provider_not_configured", body.GetProperty("lastErrorCode").GetString());
        Assert.Equal(notificationId, body.GetProperty("notificationId").GetGuid());
        var stored = Assert.Single(fixture.Data.Notifications.All, item => item.NotificationId == notificationId);
        Assert.Null(stored.DeliveredAt);
        Assert.Equal(1, stored.AttemptCount);
    }

    [Fact]
    public async Task RejectedProviderMarksTheNotificationFailedAndNeverDelivered()
    {
        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.Reject;
        var incidentId = await fixture.ReportEmergency();
        var notificationId = Guid.NewGuid();

        var (_, body) = await Request(incidentId, notificationId);

        Assert.Equal("failed", body.GetProperty("status").GetString());
        Assert.Equal("provider_validation_error", body.GetProperty("failureCode").GetString());
        var (retry, retryBody) = await fixture.Send("POST",
            $"/api/v1/incidents/{incidentId}/notifications/{notificationId}/attempts", Admin);
        Assert.Equal(409, retry);
        Assert.Equal("IncidentNotification.NotRetriable", retryBody.GetProperty("code").GetString());
        fixture.Data.Provider.DeliveryOutcome = IncidentNotificationDeliveryOutcome.Delivered;
        var (_, check) = await fixture.Send("POST",
            $"/api/v1/incidents/{incidentId}/notifications/{notificationId}/status-checks", Admin);
        Assert.Equal("failed", check.GetProperty("status").GetString());
        fixture.Data.Provider.DeliveryOutcome = IncidentNotificationDeliveryOutcome.Accepted;
    }

    [Fact]
    public async Task RetryAfterALostResponseKeepsTheIdentityAndSendsASingleProviderMessage()
    {
        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.AcceptThenLoseResponse;
        var incidentId = await fixture.ReportEmergency();
        var notificationId = Guid.NewGuid();
        var callsBefore = fixture.Data.Provider.Calls.Count;
        var messagesBefore = fixture.Data.Provider.DistinctMessages;

        var (_, uncertain) = await Request(incidentId, notificationId);
        Assert.Equal("pending", uncertain.GetProperty("status").GetString());
        Assert.Equal("provider_timeout", uncertain.GetProperty("lastErrorCode").GetString());

        // The client repeats the same request after the timeout: same identity, same provider idempotency key.
        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.Accept;
        var (repeated, accepted) = await Request(incidentId, notificationId);

        Assert.Equal(200, repeated);
        Assert.Equal("accepted", accepted.GetProperty("status").GetString());
        Assert.Equal(notificationId, accepted.GetProperty("notificationId").GetGuid());
        Assert.Equal(2, accepted.GetProperty("attemptCount").GetInt32());
        var calls = fixture.Data.Provider.Calls.Skip(callsBefore).ToArray();
        Assert.Equal(2, calls.Length);
        Assert.All(calls, call => Assert.Equal(notificationId, call.NotificationId));
        Assert.Single(calls.Select(call => call.IdempotencyKey).Distinct());
        Assert.Equal(messagesBefore + 1, fixture.Data.Provider.DistinctMessages);
        Assert.Single(fixture.Data.Notifications.All, item => item.NotificationId == notificationId);
        // Acceptance by the provider is not delivery.
        Assert.Equal(JsonNull, accepted.GetProperty("deliveredAt").ValueKind);
    }

    [Fact]
    public async Task OnlyAProviderConfirmationMovesAnAcceptedNotificationToDelivered()
    {
        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.Accept;
        var incidentId = await fixture.ReportEmergency();
        var notificationId = Guid.NewGuid();
        var (_, accepted) = await Request(incidentId, notificationId);
        Assert.Equal("accepted", accepted.GetProperty("status").GetString());
        var statusPath = $"/api/v1/incidents/{incidentId}/notifications/{notificationId}/status-checks";

        fixture.Data.Provider.DeliveryOutcome = IncidentNotificationDeliveryOutcome.Accepted;
        var (_, stillAccepted) = await fixture.Send("POST", statusPath, Admin);
        fixture.Data.Provider.DeliveryOutcome = IncidentNotificationDeliveryOutcome.Delivered;
        var (_, delivered) = await fixture.Send("POST", statusPath, Admin);
        fixture.Data.Provider.DeliveryOutcome = IncidentNotificationDeliveryOutcome.Accepted;

        Assert.Equal("accepted", stillAccepted.GetProperty("status").GetString());
        Assert.Equal("delivered", delivered.GetProperty("status").GetString());
        Assert.NotEqual(JsonNull, delivered.GetProperty("deliveredAt").ValueKind);
    }

    [Fact]
    public async Task RateLimitedAttemptStaysPendingAndCanBeRetriedWithTheSameIdentity()
    {
        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.RateLimited;
        var incidentId = await fixture.ReportEmergency();
        var notificationId = Guid.NewGuid();
        var (_, pending) = await Request(incidentId, notificationId);
        Assert.Equal("pending", pending.GetProperty("status").GetString());
        Assert.Equal("provider_rate_limited", pending.GetProperty("lastErrorCode").GetString());

        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.Accept;
        var (retried, body) = await fixture.Send("POST",
            $"/api/v1/incidents/{incidentId}/notifications/{notificationId}/attempts", Admin);

        Assert.Equal(200, retried);
        Assert.Equal("accepted", body.GetProperty("status").GetString());
        Assert.Equal(notificationId, body.GetProperty("notificationId").GetGuid());
    }

    [Fact]
    public async Task ReusedIdentityWithAnotherRecipientIsRejected()
    {
        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.Accept;
        var incidentId = await fixture.ReportEmergency();
        var notificationId = Guid.NewGuid();
        Assert.Equal(201, (await Request(incidentId, notificationId)).Status);

        var (status, body) = await fixture.Send("POST", $"/api/v1/incidents/{incidentId}/notifications", Admin,
            new { notificationId, recipientEmail = "guide-a@vitaltrek.test", recipientType = "agency_staff" });

        Assert.Equal(409, status);
        Assert.Equal("IncidentNotification.IdentityConflict", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task OnlyAdministratorsRequestCommunicationsForADeclaredEmergencyToAuthorizedRecipients()
    {
        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.Accept;
        var confirmedOnly = await fixture.ReportIncident();
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{confirmedOnly}/confirm", GuideA)).Status);
        var (beforeEmergency, beforeBody) = await Request(confirmedOnly, Guid.NewGuid());
        Assert.Equal(409, beforeEmergency);
        Assert.Equal("IncidentNotification.EmergencyNotDeclared", beforeBody.GetProperty("code").GetString());

        var incidentId = await fixture.ReportEmergency();
        Assert.Equal(403, (await Request(incidentId, Guid.NewGuid(), GuideA)).Status);
        Assert.Equal(403, (await Request(incidentId, Guid.NewGuid(), Tourist)).Status);
        Assert.Equal(404, (await Request(incidentId, Guid.NewGuid(), OtherAgencyAdmin)).Status);
        Assert.Equal(401, (await fixture.Send("GET", $"/api/v1/incidents/{incidentId}/notifications")).Status);

        // A staff account outside the allow list of team-controlled addresses, and an external address.
        foreach (var recipient in new[] { "guide-a@vitaltrek.test", "rescue@real-service.example" })
        {
            var (status, body) = await fixture.Send("POST", $"/api/v1/incidents/{incidentId}/notifications", Admin,
                new { notificationId = Guid.NewGuid(), recipientEmail = recipient });
            Assert.Equal(400, status);
            Assert.Equal("IncidentNotification.RecipientNotAllowed", body.GetProperty("code").GetString());
        }

        Assert.Equal(400, (await fixture.Send("POST", $"/api/v1/incidents/{incidentId}/notifications", Admin,
            new { recipientEmail = AllowedRecipient, recipientType = "rescue_entity" })).Status);
    }

    [Fact]
    public async Task TheMessageContainsNoDescriptionHealthOrContactData()
    {
        fixture.Data.Provider.Behaviour = FakeNotificationProvider.Mode.Accept;
        const string sensitive = "Tourist 70123456 has asthma, contact Maria at +51 999 888 777.";
        var incidentId = await fixture.ReportIncident(description: sensitive);
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{incidentId}/confirm", GuideA)).Status);
        Assert.Equal(200, (await fixture.Send("PATCH", $"/api/v1/incidents/{incidentId}/declare-emergency", Admin)).Status);
        var notificationId = Guid.NewGuid();

        await Request(incidentId, notificationId);

        var message = fixture.Data.Provider.Calls.Last(call => call.NotificationId == notificationId);
        Assert.Equal(AllowedRecipient, message.RecipientAddress);
        Assert.StartsWith("[VitalTrek exercise]", message.Subject);
        foreach (var fragment in new[] { "asthma", "70123456", "Maria", "999 888 777", "Tourist" })
        {
            Assert.DoesNotContain(fragment, message.Subject, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(fragment, message.Body, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("not a real emergency", message.Body);
    }

    private Task<(int Status, System.Text.Json.JsonElement Body)> Request(int incidentId, Guid notificationId,
        int userId = Admin) =>
        fixture.Send("POST", $"/api/v1/incidents/{incidentId}/notifications", userId,
            new { notificationId, recipientEmail = AllowedRecipient.ToUpperInvariant(), recipientType = "agency_staff" });

    private const System.Text.Json.JsonValueKind JsonNull = System.Text.Json.JsonValueKind.Null;
}
