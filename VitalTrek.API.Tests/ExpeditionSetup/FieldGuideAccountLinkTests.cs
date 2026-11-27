using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.ExpeditionSetup.Application.Errors;
using VitalTrek.API.ExpeditionSetup.Application.Internal.CommandServices;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Repositories;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.ExpeditionSetup.Interfaces.Rest.Transform;
using VitalTrek.API.IAM.Application.Errors;
using VitalTrek.API.IAM.Application.Internal.CommandServices;
using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using VitalTrek.API.IAM.Domain.Model.Aggregates;
using VitalTrek.API.IAM.Domain.Model.Commands;
using VitalTrek.API.IAM.Domain.Model.ValueObjects;
using VitalTrek.API.IAM.Domain.Repositories;
using VitalTrek.API.Shared.Application.Patterns;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.ExpeditionSetup;

/// <summary>
///     I05 and I T6: the guide profile is created once and keeps an operational code that tells homonyms apart; when the
///     account creation fails the retry links a single account to the same profile.
/// </summary>
public sealed class FieldGuideAccountLinkTests
{
    private readonly List<FieldGuide> _guides = [];
    private readonly List<User> _users = [];
    private int _nextGuideId = 1, _nextUserId = 1;

    [Fact]
    public async Task HomonymGuidesReceiveDistinctSequentialOperationalCodes()
    {
        var service = GuideService();

        var first = Created(await service.Handle(new CreateFieldGuideCommand(1, "Ana Quispe"), default));
        var second = Created(await service.Handle(new CreateFieldGuideCommand(1, "Ana Quispe"), default));

        Assert.Equal("GU-001", first.Code);
        Assert.Equal("GU-002", second.Code);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("GU-002", ExpeditionSetupResourceAssembler.ToResourceFromEntity(second).Code);
    }

    [Fact]
    public async Task ExplicitCodeIsNormalizedAndADuplicateCodeIsRejected()
    {
        var service = GuideService();
        var guide = Created(await service.Handle(new CreateFieldGuideCommand(1, "Luis Rojas", " hz-07 "), default));
        Assert.Equal("HZ-07", guide.Code);

        var duplicate = await service.Handle(new CreateFieldGuideCommand(1, "Luis Rojas", "HZ-07"), default);
        var invalid = await service.Handle(new CreateFieldGuideCommand(1, "Luis Rojas", "bad code!"), default);

        Assert.Equal(ECreateFieldGuideError.DuplicateCode,
            Assert.IsType<Result<FieldGuide, ECreateFieldGuideError>.Failure>(duplicate).Error);
        Assert.Equal(ECreateFieldGuideError.InvalidData,
            Assert.IsType<Result<FieldGuide, ECreateFieldGuideError>.Failure>(invalid).Error);
        Assert.Single(_guides);
    }

    [Fact]
    public async Task FailedAccountCreationKeepsTheProfileAndTheRetryLinksASingleAccount()
    {
        var profile = Created(await GuideService().Handle(new CreateFieldGuideCommand(1, "Rosa Mamani"), default));
        _users.Add(WithId(new User(new Username("taken@vitaltrek.test"), "hash", "Somebody", Role.FieldGuide, 1, 99), _nextUserId++));
        var accounts = UserService();

        var failed = await accounts.Handle(new CreateUserCommand(1, "Rosa Mamani", new Username("taken@vitaltrek.test"),
            "secret-1", Role.FieldGuide, profile.Id, null), default);
        var retried = await accounts.Handle(new CreateUserCommand(1, "Rosa Mamani", new Username("rosa@vitaltrek.test"),
            "secret-1", Role.FieldGuide, profile.Id, null), default);
        var secondLink = await accounts.Handle(new CreateUserCommand(1, "Rosa Mamani", new Username("rosa2@vitaltrek.test"),
            "secret-1", Role.FieldGuide, profile.Id, null), default);

        Assert.Equal(ECreateUserError.DuplicateUsername,
            Assert.IsType<Result<User, ECreateUserError>.Failure>(failed).Error);
        Assert.Equal(profile.Id, Assert.IsType<Result<User, ECreateUserError>.Success>(retried).Value.FieldGuideId);
        Assert.Equal(ECreateUserError.LinkedIdentityAlreadyExists,
            Assert.IsType<Result<User, ECreateUserError>.Failure>(secondLink).Error);
        Assert.Single(_guides);
        Assert.Single(_users, user => user.FieldGuideId == profile.Id);
    }

    [Fact]
    public async Task AccountCannotLinkAProfileOfAnotherAgency()
    {
        var profile = Created(await GuideService().Handle(new CreateFieldGuideCommand(1, "Mario Huaman"), default));

        var result = await UserService().Handle(new CreateUserCommand(2, "Mario Huaman", new Username("mario@other.test"),
            "secret-1", Role.FieldGuide, profile.Id, null), default);

        Assert.Equal(ECreateUserError.LinkedIdentityNotFound,
            Assert.IsType<Result<User, ECreateUserError>.Failure>(result).Error);
        Assert.Empty(_users);
    }

    private static FieldGuide Created(Result<FieldGuide, ECreateFieldGuideError> result) =>
        Assert.IsType<Result<FieldGuide, ECreateFieldGuideError>.Success>(result).Value;

    private FieldGuideCommandService GuideService()
    {
        var pending = new List<FieldGuide>();
        var repository = InterfaceStub<IFieldGuideRepository>.Create((method, args) => method.Name switch
        {
            nameof(IFieldGuideRepository.CountByAgencyIdAsync) =>
                Task.FromResult(_guides.Count(guide => guide.AgencyId == (int)args[0]!)),
            nameof(IFieldGuideRepository.ExistsByAgencyIdAndCodeAsync) => Task.FromResult(_guides.Any(guide =>
                guide.AgencyId == (int)args[0]! && guide.Code == (string)args[1]!)),
            nameof(IFieldGuideRepository.AddAsync) => Add(pending, (FieldGuide)args[0]!),
            _ => throw new InvalidOperationException(method.Name)
        });
        return new FieldGuideCommandService(repository, UnitOfWork(() =>
        {
            foreach (var guide in pending) _guides.Add(WithId(guide, _nextGuideId++));
            pending.Clear();
        }), NullLogger<FieldGuideCommandService>.Instance);
    }

    private UserCommandService UserService()
    {
        var pending = new List<User>();
        var users = InterfaceStub<IUserRepository>.Create((method, args) => method.Name switch
        {
            nameof(IUserRepository.ExistsByUsernameAsync) =>
                Task.FromResult(_users.Any(user => user.Username == (Username)args[0]!)),
            nameof(IUserRepository.ExistsByAgencyIdAndFieldGuideIdAsync) => Task.FromResult(_users.Any(user =>
                user.AgencyId == (int)args[0]! && user.FieldGuideId == (int)args[1]!)),
            nameof(IUserRepository.AddAsync) => Add(pending, (User)args[0]!),
            _ => throw new InvalidOperationException(method.Name)
        });
        var setup = InterfaceStub<IExpeditionSetupContextFacade>.Create((method, args) => method.Name switch
        {
            nameof(IExpeditionSetupContextFacade.ExistsFieldGuide) => Task.FromResult(_guides.Any(guide =>
                guide.AgencyId == (int)args[0]! && guide.Id == (int)args[1]!)),
            _ => throw new InvalidOperationException(method.Name)
        });
        var hashing = InterfaceStub<IHashingService>.Create((method, args) => method.Name switch
        {
            nameof(IHashingService.HashPassword) => "hash-" + args[0],
            _ => throw new InvalidOperationException(method.Name)
        });
        return new UserCommandService(users, InterfaceStub<IAgencyRepository>.Create(), hashing,
            InterfaceStub<ITokenService>.Create(), setup, UnitOfWork(() =>
            {
                foreach (var user in pending) _users.Add(WithId(user, _nextUserId++));
                pending.Clear();
            }), NullLogger<UserCommandService>.Instance);
    }

    private static Task Add<T>(List<T> pending, T item)
    {
        pending.Add(item);
        return Task.CompletedTask;
    }

    private static IUnitOfWork UnitOfWork(Action commit) => InterfaceStub<IUnitOfWork>.Create((method, _) =>
    {
        if (method.Name != nameof(IUnitOfWork.CompleteAsync)) throw new InvalidOperationException(method.Name);
        commit();
        return Task.CompletedTask;
    });

    private static T WithId<T>(T entity, int id)
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}
