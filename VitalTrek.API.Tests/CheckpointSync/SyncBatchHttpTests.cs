using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VitalTrek.API.CheckpointSync.Application.Internal.CommandServices;
using VitalTrek.API.CheckpointSync.Application.Internal.OutboundServices.ACL;
using VitalTrek.API.CheckpointSync.Application.Internal.QueryServices;
using VitalTrek.API.CheckpointSync.Application.Services;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.CheckpointSync.Interfaces.Rest;
using VitalTrek.API.ExpeditionSetup.Interfaces.ACL;
using VitalTrek.API.FieldTracking.Application.ACL;
using VitalTrek.API.FieldTracking.Application.Internal.CommandServices;
using VitalTrek.API.FieldTracking.Application.Services;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.FieldTracking.Interfaces.ACL;
using VitalTrek.API.IAM.Application.Internal.OutboundServices;
using VitalTrek.API.IAM.Application.Services;
using VitalTrek.API.IAM.Domain.Model.Queries;
using VitalTrek.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using VitalTrek.API.IAM.Infrastructure.Tokens.JWT.Services;
using VitalTrek.API.SafetyMonitoring.Interfaces.ACL;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Shared.Infrastructure.Interfaces.Asp.Configuration;
using VitalTrek.API.Tests.FieldTracking;
using VitalTrek.API.Tests.Support;
using Xunit;

namespace VitalTrek.API.Tests.CheckpointSync;

/// <summary>S01: sync batch reception through the real HTTP pipeline, IAM middleware and the M03 telemetry port.</summary>
public sealed class SyncBatchHttpTests
{
    private const string Path = "/api/v1/sync-batches";
    private const int Guide = 2, OtherGuide = 3, Admin = 1, Tourist = 4, OtherAgencyAdmin = 5;
    // Whole seconds: the stored capture keeps microsecond precision, like the MySQL column.
    private static readonly DateTimeOffset CapturedAt =
        DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeSeconds());

    [Fact]
    public Task ValidBatchIsCommittedOnceAndKeepsTheOriginalCaptureTimes() => WithApi(async (data, api) =>
    {
        var (batchId, readingId, passageId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        using var response = await api.Send("POST", Path, Guide,
            Batch(batchId, [Reading(readingId)], [Passage(passageId)]));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await Json(response);
        var id = body.GetProperty("batchId").GetInt32();
        Assert.EndsWith($"{Path}/{id}", response.Headers.Location!.ToString());
        Assert.Equal(batchId, body.GetProperty("clientBatchId").GetGuid());
        Assert.Equal(Ids(readingId, passageId), Guids(body, "acceptedRecordIds"));
        Assert.Empty(Guids(body, "duplicateRecordIds"));
        Assert.Equal(0, body.GetProperty("rejectedRecords").GetArrayLength());
        Assert.Equal("pending", body.GetProperty("evaluationStatus").GetString());

        Assert.Equal(1, data.Saves);
        var reading = Assert.Single(data.StoredReadings);
        Assert.Equal(readingId, reading.ClientRecordId);
        Assert.Equal(60, reading.MemberId);
        Assert.Equal(CapturedAt, reading.RecordedAt);
        Assert.True(reading.SynchronizedAt > reading.RecordedAt);
        var passage = Assert.Single(data.StoredPassages);
        Assert.Equal(CapturedAt.AddMinutes(1), passage.PassedAt);
        Assert.Equal(10, passage.FieldGuideId);
        Assert.Single(data.StoredBatches);
    });

    [Fact]
    public Task IdenticalRetryReturnsTheSameResultWithoutNewRowsEvenAfterTheSessionCloses() => WithApi(async (data, api) =>
    {
        var batch = Batch(Guid.NewGuid(), [Reading(Guid.NewGuid())], [Passage(Guid.NewGuid())]);
        using var first = await api.Send("POST", Path, Guide, batch);
        using var retry = await api.Send("POST", Path, Guide, batch);
        data.Session.End();
        using var afterClosure = await api.Send("POST", Path, Guide, batch);

        var expected = await first.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Accepted, retry.StatusCode);
        Assert.Equal(expected, await retry.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Accepted, afterClosure.StatusCode);
        Assert.Equal(expected, await afterClosure.Content.ReadAsStringAsync());
        Assert.Equal(1, data.Saves);
        Assert.Single(data.StoredReadings);
        Assert.Single(data.StoredPassages);
        Assert.Single(data.StoredBatches);
    });

    [Fact]
    public Task ReusedBatchIdentityWithDifferentContentReturns409AndKeepsTheAcceptedData() => WithApi(async (data, api) =>
    {
        data.SeedArrival(30, CapturedAt);
        var (batchId, readingId) = (Guid.NewGuid(), Guid.NewGuid());
        using var first = await api.Send("POST", Path, Guide, Batch(batchId, [Reading(readingId)], []));
        using var conflict = await api.Send("POST", Path, Guide, Batch(batchId, [Reading(readingId, heartRate: 99)], []));

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("SyncBatch.BatchIdentityConflict", (await Json(conflict)).GetProperty("code").GetString());
        Assert.Equal(88, Assert.Single(data.StoredReadings).HeartRate);
        Assert.Equal(1, data.Saves);
    });

    [Fact]
    public Task InvalidRowsAreRejectedWithCodesWhileIndependentValidRowsPersist() => WithApi(async (data, api) =>
    {
        var (valid, foreignWearable, invalidBattery, validPassage, offRoute) =
            (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        data.Ready.Preparations[1].RevokeConsent(DateTimeOffset.UtcNow);
        var withoutConsent = Guid.NewGuid();
        using var response = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(),
            [Reading(valid), Reading(foreignWearable, wearable: 999), Reading(invalidBattery, battery: 101),
                Reading(withoutConsent, wearable: 71)],
            [Passage(validPassage), Passage(offRoute, checkpoint: 999)]));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await Json(response);
        Assert.Equal(Ids(valid, validPassage), Guids(body, "acceptedRecordIds"));
        var rejected = body.GetProperty("rejectedRecords").EnumerateArray().ToDictionary(
            record => record.GetProperty("clientRecordId").GetGuid(), record => record.GetProperty("code").GetString());
        Assert.Equal(4, rejected.Count);
        Assert.Equal("Telemetry.WearableNotAssigned", rejected[foreignWearable]);
        Assert.Equal("Telemetry.InvalidBattery", rejected[invalidBattery]);
        Assert.Equal("Telemetry.ConsentRequired", rejected[withoutConsent]);
        Assert.Equal("Passage.CheckpointNotInRoute", rejected[offRoute]);
        Assert.Equal(valid, Assert.Single(data.StoredReadings).ClientRecordId);
        Assert.Single(data.StoredPassages);
        Assert.Equal(1, data.Saves);
    });

    [Fact]
    public Task RecordsStoredByAnEarlierBatchAreReportedAsDuplicatesAndKeepTheFirstEvidence() => WithApi(async (data, api) =>
    {
        var (readingId, firstPassage, laterPassage) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        using var first = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [Reading(readingId)], [Passage(firstPassage)]));
        using var second = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [Reading(readingId)],
            [Passage(laterPassage, passedAt: CapturedAt.AddMinutes(5))]));

        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        var body = await Json(second);
        Assert.Empty(Guids(body, "acceptedRecordIds"));
        Assert.Equal(Ids(readingId, laterPassage), Guids(body, "duplicateRecordIds"));
        Assert.Single(data.StoredReadings);
        Assert.Equal(CapturedAt.AddMinutes(1), Assert.Single(data.StoredPassages).PassedAt);
        Assert.Equal(2, data.StoredBatches.Count);
    });

    [Fact]
    public Task ReusedRecordIdentityWithDifferentDataRejectsTheWholeBatch() => WithApi(async (data, api) =>
    {
        data.SeedArrival(30, CapturedAt);
        var readingId = Guid.NewGuid();
        using var first = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(), [Reading(readingId)], []));
        using var conflict = await api.Send("POST", Path, Guide, Batch(Guid.NewGuid(),
            [Reading(Guid.NewGuid()), Reading(readingId, heartRate: 99)], [Passage(Guid.NewGuid())]));

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("SyncBatch.RecordIdentityConflict", (await Json(conflict)).GetProperty("code").GetString());
        Assert.Equal(88, Assert.Single(data.StoredReadings).HeartRate);
        Assert.Single(data.StoredPassages);
        Assert.Single(data.StoredBatches);
    });

    [Theory]
    [InlineData("closed", HttpStatusCode.Conflict, "SyncBatch.SessionNotActive")]
    [InlineData("otherGuide", HttpStatusCode.NotFound, "SyncBatch.SessionNotFound")]
    [InlineData("unknownSession", HttpStatusCode.NotFound, "SyncBatch.SessionNotFound")]
    [InlineData("checkpoint", HttpStatusCode.BadRequest, "SyncBatch.CheckpointNotInSessionRoute")]
    [InlineData("repeatedIds", HttpStatusCode.BadRequest, "SyncBatch.InvalidData")]
    [InlineData("empty", HttpStatusCode.BadRequest, "SyncBatch.InvalidData")]
    public Task BatchLevelFailuresRejectTheWholeBatchWithoutPartialChanges(string fault, HttpStatusCode expected,
        string code) => WithApi(async (data, api) =>
    {
        var sharedId = Guid.NewGuid();
        if (fault == "closed") data.Session.End();
        object batch = fault switch
        {
            "unknownSession" => Batch(Guid.NewGuid(), [Reading(Guid.NewGuid())], [], sessionId: 999),
            "checkpoint" => Batch(Guid.NewGuid(), [Reading(Guid.NewGuid())], [], checkpoint: 999),
            "repeatedIds" => Batch(Guid.NewGuid(), [Reading(sharedId)], [Passage(sharedId)]),
            "empty" => Batch(Guid.NewGuid(), [], []),
            _ => Batch(Guid.NewGuid(), [Reading(Guid.NewGuid())], [Passage(Guid.NewGuid())])
        };

        using var response = await api.Send("POST", Path, fault == "otherGuide" ? OtherGuide : Guide, batch);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(code, (await Json(response)).GetProperty("code").GetString());
        AssertNothingStored(data);
    });

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(Admin, HttpStatusCode.Forbidden)]
    [InlineData(Tourist, HttpStatusCode.Forbidden)]
    public Task OnlyAnAuthenticatedFieldGuideCanSubmit(int? user, HttpStatusCode expected) => WithApi(async (data, api) =>
    {
        using var response = await api.Send("POST", Path, user, Batch(Guid.NewGuid(), [Reading(Guid.NewGuid())], []));

        Assert.Equal(expected, response.StatusCode);
        AssertNothingStored(data);
    });

    [Fact]
    public Task MissingCaptureFieldsAreRejectedInsteadOfInventingValues() => WithApi(async (data, api) =>
    {
        var reading = new Dictionary<string, object?>
        {
            ["clientRecordId"] = Guid.NewGuid(), ["wearableDeviceId"] = 70, ["recordedAt"] = CapturedAt,
            ["latitude"] = -9.5, ["longitude"] = -77.5, ["altitude"] = 3000, ["heartRate"] = 88, ["battery"] = 85
        };
        using var response = await api.Send("POST", Path, Guide, new
        {
            clientBatchId = Guid.NewGuid(), sessionId = 501, checkpointId = 30, readings = new[] { reading }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertNothingStored(data);
    });

    [Fact]
    public Task StorageFailureLeavesNoBatchNorRecordsAndTheSameBatchCanBeRetried() => WithApi(async (data, api) =>
    {
        var batch = Batch(Guid.NewGuid(), [Reading(Guid.NewGuid())], [Passage(Guid.NewGuid())]);
        data.BeforeNextSave = () => throw new InvalidOperationException("Simulated storage failure.");
        using var failed = await api.Send("POST", Path, Guide, batch);

        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        AssertNothingStored(data);
        Assert.Empty(data.PendingBatches);
        Assert.Empty(data.Telemetry.Pending);

        using var retry = await api.Send("POST", Path, Guide, batch);
        Assert.Equal(HttpStatusCode.Accepted, retry.StatusCode);
        Assert.Single(data.StoredBatches);
        Assert.Single(data.StoredReadings);
        Assert.Single(data.StoredPassages);
    });

    [Fact]
    public Task ConcurrentCommitOfTheSameBatchReturnsTheWinnerResult() => WithApi(async (data, api) =>
    {
        var batch = Batch(Guid.NewGuid(), [Reading(Guid.NewGuid())], [Passage(Guid.NewGuid())]);
        data.BeforeNextSave = () =>
        {
            // Another request commits the same staged batch first; this save hits the unique index.
            data.CommitPending();
            throw new DbUpdateException("Duplicate entry", new MySqlException(1062));
        };

        using var response = await api.Send("POST", Path, Guide, batch);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var winner = Assert.Single(data.StoredBatches);
        Assert.Equal(winner.Id, (await Json(response)).GetProperty("batchId").GetInt32());
        Assert.Single(data.StoredReadings);
        Assert.Single(data.StoredPassages);
    });

    [Theory]
    [InlineData(Guide, HttpStatusCode.OK)]
    [InlineData(Admin, HttpStatusCode.OK)]
    [InlineData(OtherGuide, HttpStatusCode.NotFound)]
    [InlineData(OtherAgencyAdmin, HttpStatusCode.NotFound)]
    [InlineData(Tourist, HttpStatusCode.Forbidden)]
    public Task BatchResultIsReadableOnlyWithinTheAgencyAndAssignedGroup(int user, HttpStatusCode expected) =>
        WithApi(async (_, api) =>
        {
            using var created = await api.Send("POST", Path, Guide,
                Batch(Guid.NewGuid(), [Reading(Guid.NewGuid())], [Passage(Guid.NewGuid())]));
            var expectedBody = await created.Content.ReadAsStringAsync();

            using var response = await api.Send("GET", created.Headers.Location!.AbsolutePath, user);

            Assert.Equal(expected, response.StatusCode);
            if (expected == HttpStatusCode.OK) Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
        });

    private static void AssertNothingStored(SyncBatchData data)
    {
        Assert.Empty(data.StoredBatches);
        Assert.Empty(data.StoredReadings);
        Assert.Empty(data.StoredPassages);
        Assert.Equal(0, data.Saves);
    }

    private static object Batch(Guid clientBatchId, object[] readings, object[] passages, int sessionId = 501,
        int checkpoint = 30) => new
    {
        clientBatchId, sessionId, checkpointId = checkpoint, readings, passages, agencyId = 999, fieldGuideId = 999
    };

    private static object Reading(Guid id, int wearable = 70, int heartRate = 88, int battery = 85) => new
    {
        clientRecordId = id, wearableDeviceId = wearable, recordedAt = CapturedAt, latitude = -9.5, longitude = -77.5,
        altitude = 3000, heartRate, battery, deviationMeters = 12.5, memberId = 999
    };

    private static object Passage(Guid id, int checkpoint = 30, DateTimeOffset? passedAt = null) => new
    {
        clientRecordId = id, checkpointId = checkpoint, passedAt = passedAt ?? CapturedAt.AddMinutes(1)
    };

    private static Guid[] Ids(params Guid[] ids) => ids.Order().ToArray();

    private static Guid[] Guids(JsonElement body, string property) =>
        body.GetProperty(property).EnumerateArray().Select(item => item.GetGuid()).ToArray();

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private static async Task WithApi(Func<SyncBatchData, SyncBatchApi, Task> assertion)
    {
        var data = await SyncBatchData.CreateAsync();
        await using var api = await SyncBatchApi.StartAsync(data);
        await assertion(data, api);
    }

    /// <summary>Same type name and error number the shared duplicate-key detection inspects.</summary>
    private sealed class MySqlException(int number) : Exception("Simulated MySQL error.")
    {
        public int Number { get; } = number;
    }
}

/// <summary>Production controllers, IAM middleware and services over the in-memory <see cref="SyncBatchData"/>.</summary>
internal sealed class SyncBatchApi : IAsyncDisposable
{
    private const string Secret = "vitaltrek-s01-test-signing-key-only-not-a-production-secret-1234567890";
    private readonly WebApplication _app;
    private readonly HttpClient _client;
    private readonly TrackingAccessData _users = new();

    private SyncBatchApi(WebApplication app)
    {
        _app = app;
        _client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(15) };
    }

    public static async Task<SyncBatchApi> StartAsync(SyncBatchData data,
        ISafetyMonitoringContextFacade? safetyMonitoring = null)
    {
        var users = new TrackingAccessData();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddLocalization();
        builder.Services.AddProblemDetails();
        builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()))
            .AddApplicationPart(typeof(SyncBatchesController).Assembly);
        builder.Services.Configure<TokenSettings>(settings => settings.Secret = Secret);
        builder.Services.AddSingleton<ITokenService, TokenService>();
        builder.Services.AddSingleton(InterfaceStub<IUserQueryService>.Create((method, args) =>
            args[0] is GetUserByIdQuery query
                ? Task.FromResult(users.Users.FirstOrDefault(user => user.Id == query.Id))
                : throw new InvalidOperationException(method.Name)));
        builder.Services.AddSingleton<IExpeditionSetupContextFacade>(data.Ready.Expeditions);
        builder.Services.AddSingleton(data.Sessions);
        builder.Services.AddSingleton(data.TelemetryRepository);
        builder.Services.AddSingleton(data.PassageRepository);
        builder.Services.AddSingleton(data.BatchRepository);
        builder.Services.AddSingleton(data.UnitOfWork);
        builder.Services.AddScoped<ITelemetryRecordCommandService, TelemetryRecordCommandService>();
        builder.Services.AddScoped<IFieldTrackingContextFacade, FieldTrackingContextFacade>();
        builder.Services.AddScoped<ExternalFieldTrackingService>();
        builder.Services.AddScoped<ICheckpointPassageCommandService, CheckpointPassageCommandService>();
        builder.Services.AddScoped<ICheckpointPassageQueryService, CheckpointPassageQueryService>();
        builder.Services.AddScoped<ISyncBatchCommandService, SyncBatchCommandService>();
        if (safetyMonitoring is not null) builder.Services.AddSingleton(safetyMonitoring);
        builder.Services.AddScoped<ISyncBatchQueryService, SyncBatchQueryService>();
        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseRequestAuthorization();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        return new SyncBatchApi(app);
    }

    public async Task<HttpResponseMessage> Send(string method, string path, int? userId, object? body = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (userId is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                _app.Services.GetRequiredService<ITokenService>().GenerateToken(
                    _users.Users.Single(user => user.Id == userId)));
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
