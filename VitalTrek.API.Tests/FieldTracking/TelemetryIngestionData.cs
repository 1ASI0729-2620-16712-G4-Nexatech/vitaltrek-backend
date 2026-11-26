using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using VitalTrek.API.FieldTracking.Application.Internal.CommandServices;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Model.Commands;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Tests.Support;
using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Repositories;

namespace VitalTrek.API.Tests.FieldTracking;

internal sealed class TelemetryIngestionData
{
    public TrackingStartData Ready { get; } = new();
    public List<TelemetryRecord> Stored { get; } = [];
    public List<TelemetryRecord> Pending { get; } = [];
    public List<CheckpointPassage> Passages { get; } = [];
    public ICheckpointPassageRepository PassageRepository { get; }
    public int Saves { get; private set; }
    public ITelemetryRecordRepository Repository { get; }
    public IUnitOfWork UnitOfWork { get; }

    private TelemetryIngestionData()
    {
        Repository = InterfaceStub<ITelemetryRecordRepository>.Create((method, args) => method.Name switch
        {
            nameof(ITelemetryRecordRepository.FindLatestForMembersAsync) => Task.FromResult<IReadOnlyList<TelemetryRecord>>(
                Stored.Where(record => record.SessionId == (int)args[0]! && record.MemberId.HasValue &&
                    ((IReadOnlyCollection<int>)args[1]!).Contains(record.MemberId.Value))
                    .OrderByDescending(record => record.RecordedAt).ThenByDescending(record => record.Id)
                    .GroupBy(record => record.MemberId).Select(group => group.First()).ToArray()),
            nameof(ITelemetryRecordRepository.FindLastSynchronizationAsync) => Task.FromResult(Stored
                .Where(record => record.SessionId == (int)args[0]!).Select(record => (DateTimeOffset?)record.SynchronizedAt).Max()),
            nameof(ITelemetryRecordRepository.FindByClientRecordIdAsync) => Task.FromResult(Pending.Concat(Stored)
                .FirstOrDefault(record => record.SessionId == (int)args[0]! && record.ClientRecordId == (Guid)args[1]!)),
            nameof(ITelemetryRecordRepository.FindAllBySessionIdAsync) => Task.FromResult<IEnumerable<TelemetryRecord>>(
                Stored.Where(record => record.SessionId == (int)args[0]!).ToArray()),
            nameof(ITelemetryRecordRepository.AddRangeAsync) => Add((IEnumerable<TelemetryRecord>)args[0]!),
            _ => throw new InvalidOperationException(method.Name)
        });
        PassageRepository = InterfaceStub<ICheckpointPassageRepository>.Create((method, args) => method.Name switch
        {
            nameof(ICheckpointPassageRepository.FindBySessionIdAndAgencyIdAsync) => Task.FromResult<IEnumerable<CheckpointPassage>>(
                Passages.Where(passage => passage.SessionId == (int)args[0]! && passage.AgencyId == (int)args[1]!).ToArray()),
            _ => throw new InvalidOperationException(method.Name)
        });
        UnitOfWork = InterfaceStub<IUnitOfWork>.Create((method, _) =>
        {
            if (method.Name == nameof(IUnitOfWork.DiscardChanges)) { Pending.Clear(); return null; }
            if (method.Name != nameof(IUnitOfWork.CompleteAsync)) throw new InvalidOperationException(method.Name);
            for (var index = 0; index < Pending.Count; index++)
                TrackingStartData.WithId(Pending[index], 300 + Stored.Count + index + 1);
            Stored.AddRange(Pending);
            Pending.Clear();
            Saves++;
            return Task.CompletedTask;
        });
    }

    public static async Task<TelemetryIngestionData> CreateAsync()
    {
        var data = new TelemetryIngestionData();
        await TrackingSessionStartTests.Start(data.Ready);
        var startedAt = DateTimeOffset.UtcNow.AddHours(-1);
        TrackingStartData.Set(data.Ready.Session!, "StartedAt", startedAt);
        TrackingStartData.Set(data.Ready.Group, "StartedAt", startedAt);
        return data;
    }

    public TelemetryRecordCommandService Service() => new(Ready.Sessions, Repository, Ready.Expeditions,
        UnitOfWork, NullLogger<TelemetryRecordCommandService>.Instance);

    public TelemetryRecordEntry Reading(Guid? identity = null) => new(-9.5, -77.5, 3000,
        DateTimeOffset.UtcNow.AddMinutes(-10), identity ?? Guid.NewGuid(), 88, 85, 12.5);

    private Task Add(IEnumerable<TelemetryRecord> records) { Pending.AddRange(records); return Task.CompletedTask; }
}
