using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;
using VitalTrek.API.IncidentResponse.Interfaces.Rest.Resources;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Tests.FieldTracking;
using VitalTrek.API.Tests.Support;
using VitalTrek.API.TourClosure.Interfaces.Rest.Resources;
using Xunit;

namespace VitalTrek.API.Tests.ExpeditionSetup;

/// <summary>
///     I04 and I T5: the optional health information belongs to the participant. Tourist A cannot read or edit the health
///     information or the contact of tourist B, and the data never reaches summaries or notification resources.
/// </summary>
public sealed class ParticipantHealthInformationTests
{
    private readonly TrackingStartData _data = new();
    private int _saves;

    [Fact]
    public async Task TouristSavesAndClearsTheirOwnHealthInformation()
    {
        var service = Service();

        var saved = await service.Handle(new RegisterHealthInformationCommand(1, 80, "  Allergic to penicillin.  ",
            "DOC-0"), default);
        var preparation = Assert.IsType<Result<ParticipantPreparation, EParticipantPreparationError>.Success>(saved).Value;
        Assert.Equal("Allergic to penicillin.", preparation.HealthNotes);
        Assert.NotNull(preparation.HealthNotesRecordedAt);

        var cleared = await service.Handle(new RegisterHealthInformationCommand(1, 80, "   ", "DOC-0"), default);
        Assert.Null(Assert.IsType<Result<ParticipantPreparation, EParticipantPreparationError>.Success>(cleared)
            .Value.HealthNotes);
        Assert.Equal(2, _saves);
    }

    [Fact]
    public async Task TouristCannotEditTheHealthInformationOrContactOfAnotherParticipant()
    {
        var service = Service();
        var other = _data.Preparations.Single(preparation => preparation.Id == 81);
        var contactBefore = other.EmergencyContact;

        var health = await service.Handle(new RegisterHealthInformationCommand(1, 81, "Injected text.", "DOC-0"), default);
        var contact = await service.Handle(new RegisterEmergencyContactCommand(1, 81, "Intruder", "+51900000000", null,
            "DOC-0"), default);

        Assert.Equal(EParticipantPreparationError.ParticipantPreparationNotFound,
            Assert.IsType<Result<ParticipantPreparation, EParticipantPreparationError>.Failure>(health).Error);
        Assert.Equal(EParticipantPreparationError.ParticipantPreparationNotFound,
            Assert.IsType<Result<ParticipantPreparation, EParticipantPreparationError>.Failure>(contact).Error);
        Assert.Null(other.HealthNotes);
        Assert.Equal(contactBefore, other.EmergencyContact);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public async Task TooLongHealthInformationIsRejectedWithoutSaving()
    {
        var result = await Service().Handle(new RegisterHealthInformationCommand(1, 80, new string('x', 501), "DOC-0"),
            default);

        Assert.Equal(EParticipantPreparationError.InvalidData,
            Assert.IsType<Result<ParticipantPreparation, EParticipantPreparationError>.Failure>(result).Error);
        Assert.Equal(0, _saves);
    }

    [Fact]
    public void PreparationResourceCarriesTheHealthInformationOnlyForTheReturnedPreparation()
    {
        var preparation = _data.Preparations[0];
        preparation.RegisterHealthInformation("Asthma inhaler in the backpack.", DateTimeOffset.UtcNow);

        var resource = ExpeditionSetupResourceAssembler.ToResourceFromEntity(preparation);

        Assert.Equal("Asthma inhaler in the backpack.", resource.HealthNotes);
        Assert.Equal(preparation.MemberId, resource.MemberId);
    }

    [Theory]
    [InlineData(typeof(TourSummaryResource))]
    [InlineData(typeof(CloseTourResource))]
    [InlineData(typeof(IncidentNotificationResource))]
    [InlineData(typeof(IncidentResource))]
    public void SummariesAndNotificationsExposeNoHealthOrContactFields(Type resource)
    {
        var names = resource.GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain(names, name => name.Contains("Health", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("EmergencyContact", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Equals("Body", StringComparison.OrdinalIgnoreCase));
    }

    private ParticipantPreparationCommandService Service()
    {
        var unitOfWork = InterfaceStub<IUnitOfWork>.Create((method, _) => method.Name switch
        {
            nameof(IUnitOfWork.CompleteAsync) => Saved(),
            _ => throw new InvalidOperationException(method.Name)
        });
        // Same scope rule as the persistence query: a tourist only reaches the preparations of its own manifest entry.
        var preparations = InterfaceStub<IParticipantPreparationRepository>.Create((method, args) => method.Name switch
        {
            nameof(IParticipantPreparationRepository.FindByIdAndScopeAsync) => Task.FromResult(_data.Preparations
                .FirstOrDefault(preparation => preparation.Id == (int)args[0]! && InScope(preparation, (AccessScope)args[1]!))),
            nameof(IParticipantPreparationRepository.Update) => null,
            _ => throw new InvalidOperationException(method.Name)
        });
        var groups = InterfaceStub<IExpeditionGroupRepository>.Create((method, args) => method.Name switch
        {
            nameof(IExpeditionGroupRepository.FindByIdAndScopeAsync) =>
                Task.FromResult<ExpeditionGroup?>((int)args[0]! == _data.Group.Id ? _data.Group : null),
            _ => throw new InvalidOperationException(method.Name)
        });
        return new ParticipantPreparationCommandService(preparations, groups,
            InterfaceStub<IWearableDeviceRepository>.Create(), unitOfWork,
            NullLogger<ParticipantPreparationCommandService>.Instance);
    }

    private Task Saved()
    {
        _saves++;
        return Task.CompletedTask;
    }

    private bool InScope(ParticipantPreparation preparation, AccessScope scope) =>
        preparation.AgencyId == scope.AgencyId &&
        (scope.IdentityDocument is null || _data.Members.Any(member =>
            member.Id == preparation.MemberId && member.IdentityDocument == scope.IdentityDocument));
}
