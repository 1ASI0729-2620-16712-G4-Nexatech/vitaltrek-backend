using VitalTrek.API.IncidentResponse.Application.Internal.OutboundServices.Notifications;
using VitalTrek.API.IncidentResponse.Domain.Model.Aggregates;
using VitalTrek.API.IncidentResponse.Domain.Model.Commands;
using VitalTrek.API.IncidentResponse.Domain.Model.ValueObjects;
using VitalTrek.API.IncidentResponse.Domain.Services;
using Xunit;

namespace VitalTrek.API.Tests.IncidentResponse.Domain;

/// <summary>I03: a notification starts pending, acceptance is not delivery and an attempt in progress is exclusive.</summary>
public sealed class IncidentNotificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(90);

    [Fact]
    public void TheAggregateStartsPendingAndIsNeverSentByItsConstructor()
    {
        var notification = Create();

        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(0, notification.AttemptCount);
        Assert.Null(notification.ProviderMessageId);
        Assert.Equal("team-test@vitaltrek.test", notification.RecipientAddress);
        Assert.Equal(IncidentNotification.EmailChannel, notification.Channel);
    }

    [Fact]
    public void AcceptanceIsNotDeliveryAndOnlyAnAcceptedMessageCanBeDelivered()
    {
        var notification = Create();
        Assert.Throws<InvalidOperationException>(() => notification.MarkDelivered(Now));

        notification.StartAttempt(Now, Lease);
        notification.MarkAccepted("msg_1", Now.AddSeconds(1));
        Assert.Equal(NotificationStatus.Accepted, notification.Status);
        Assert.Null(notification.DeliveredAt);

        notification.MarkDelivered(Now.AddMinutes(1));
        Assert.Equal(NotificationStatus.Delivered, notification.Status);
        Assert.Throws<InvalidOperationException>(() => notification.MarkFailed("late_bounce", Now.AddMinutes(2)));
    }

    [Fact]
    public void AnAttemptInProgressBlocksAnotherUntilItFinishesOrItsLeaseExpires()
    {
        var notification = Create();
        notification.StartAttempt(Now, Lease);

        Assert.False(notification.CanStartAttempt(Now.AddSeconds(10), Lease));
        Assert.Throws<InvalidOperationException>(() => notification.StartAttempt(Now.AddSeconds(10), Lease));
        Assert.True(notification.CanStartAttempt(Now.AddSeconds(91), Lease));

        notification.MarkAttemptUncertain("provider_timeout", Now.AddSeconds(20));
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.True(notification.CanStartAttempt(Now.AddSeconds(21), Lease));
        notification.StartAttempt(Now.AddSeconds(21), Lease);
        Assert.Equal(2, notification.AttemptCount);
    }

    [Fact]
    public void FailedIsTerminal()
    {
        var notification = Create();
        notification.StartAttempt(Now, Lease);
        notification.MarkFailed("provider_rejected", Now.AddSeconds(1));

        Assert.False(notification.CanStartAttempt(Now.AddMinutes(5), Lease));
        Assert.Throws<InvalidOperationException>(() => notification.MarkAccepted("msg", Now.AddMinutes(5)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-address")]
    [InlineData("a@b@c.test")]
    [InlineData("one@test.example, two@test.example")]
    [InlineData("Name <one@test.example>")]
    public void RecipientMustBeASinglePlainAddress(string address)
    {
        Assert.Throws<ArgumentException>(() => IncidentNotification.NormalizeAddress(address));
    }

    [Fact]
    public void LegacySentRowsAreReadAsFailedNeverAsDelivered()
    {
        Assert.Equal(NotificationStatus.Failed, NotificationStatusExtensions.FromCode("sent"));
        Assert.Equal(NotificationStatus.Pending, NotificationStatusExtensions.FromCode("pending"));
    }

    [Fact]
    public void IdempotencyKeyDerivesOnlyFromTheNotificationIdentity()
    {
        var id = Guid.NewGuid();
        Assert.Equal(IncidentNotificationMessage.IdempotencyKeyFor(id), IncidentNotificationMessage.IdempotencyKeyFor(id));
        Assert.NotEqual(IncidentNotificationMessage.IdempotencyKeyFor(id),
            IncidentNotificationMessage.IdempotencyKeyFor(Guid.NewGuid()));
    }

    [Fact]
    public void ContentOmitsTheFreeTextDescription()
    {
        var incident = IncidentLifecycleTests.InState(IncidentStatus.EmergencyDeclared);
        var (subject, body) = IncidentNotificationContent.Build(incident);

        Assert.DoesNotContain(incident.Description, body);
        Assert.DoesNotContain("Fall", subject + body);
        Assert.Contains("emergency_declared", body);
    }

    private static IncidentNotification Create() =>
        new(new SendNotificationCommand(1, 7, Guid.NewGuid(), NotificationRecipientType.AgencyStaff,
                " Team-Test@VitalTrek.test "), 1, "[VitalTrek exercise] subject", "body",
            IncidentContentFingerprint.ForNotification(7, NotificationRecipientType.AgencyStaff, "team-test@vitaltrek.test"),
            Now);
}
