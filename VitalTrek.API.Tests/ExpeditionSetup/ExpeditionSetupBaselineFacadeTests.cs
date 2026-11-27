using VitalTrek.API.ExpeditionSetup.Application.ACL;
using VitalTrek.API.ExpeditionSetup.Application.Services;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL.Resources;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.ExpeditionSetup;

/// <summary>
///     The baselines exposed to Safety Monitoring are limited to one group of one agency and carry no other
///     preparation data.
/// </summary>
public class ExpeditionSetupBaselineFacadeTests
{
    private static readonly ParticipantPreparation[] Preparations =
    [
        new(1, 5, 60),
        new(1, 5, 61),
        new(1, 6, 70),
        new(2, 5, 80)
    ];

    private AccessScope? _lastScope;

    [Fact]
    public async Task ReturnsOnlyTheBaselinesOfTheGroupParticipantsInTheAgency()
    {
        var baselines = await Facade().FindParticipantBaselinesByExpeditionGroup(1, 5);

        Assert.Equal(
            new[]
            {
                new ParticipantBaseline(60, ParticipantPreparation.DefaultBaselineMin, ParticipantPreparation.DefaultBaselineMax),
                new ParticipantBaseline(61, ParticipantPreparation.DefaultBaselineMin, ParticipantPreparation.DefaultBaselineMax)
            },
            baselines);
        Assert.Equal(new AccessScope(1), _lastScope);
    }

    [Fact]
    public async Task GroupOutsideTheAgencyReturnsNoBaselines()
    {
        Assert.Empty(await Facade().FindParticipantBaselinesByExpeditionGroup(3, 5));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 0)]
    public async Task InvalidIdentifiersReturnNoBaselinesWithoutQuerying(int agencyId, int expeditionGroupId)
    {
        // The default stub throws on any call, so an empty result proves the repository was not queried.
        var facade = Facade(InterfaceStub<IParticipantPreparationRepository>.Create());
        Assert.Empty(await facade.FindParticipantBaselinesByExpeditionGroup(agencyId, expeditionGroupId));
    }

    private ExpeditionSetupContextFacade Facade(IParticipantPreparationRepository? preparations = null) =>
        new(InterfaceStub<IFieldGuideRepository>.Create(), InterfaceStub<IManifestEntryRepository>.Create(),
            InterfaceStub<IExpeditionGroupRepository>.Create(), InterfaceStub<ICheckpointRepository>.Create(),
            InterfaceStub<IExpectedTimeWindowRepository>.Create(), InterfaceStub<IExpeditionStartService>.Create(),
            preparations ?? PreparationRepository(), InterfaceStub<IWearableDeviceRepository>.Create());

    private IParticipantPreparationRepository PreparationRepository() =>
        InterfaceStub<IParticipantPreparationRepository>.Create((method, args) =>
        {
            if (method.Name != nameof(IParticipantPreparationRepository.FindByScopeAsync))
                throw new InvalidOperationException(method.Name);
            _lastScope = (AccessScope)args[0]!;
            var groupId = (int?)args[1];
            return Task.FromResult<IEnumerable<ParticipantPreparation>>(Preparations
                .Where(preparation => preparation.AgencyId == _lastScope.AgencyId &&
                                      (groupId == null || preparation.ExpeditionGroupId == groupId))
                .ToArray());
        });
}
