using VitalTrek.API.CheckpointSync.Domain.Model.Aggregates;
using VitalTrek.API.CheckpointSync.Domain.Repositories;
using VitalTrek.API.FieldTracking.Domain.Model.Aggregates;
using VitalTrek.API.FieldTracking.Domain.Repositories;
using VitalTrek.API.Shared.Domain.Repositories;
using VitalTrek.API.Tests.FieldTracking;
using VitalTrek.API.Tests.Support;

namespace VitalTrek.API.Tests.CheckpointSync;

/// <summary>
///     Real preparation, session and telemetry staging with in-memory repositories. The unit of work commits readings,
///     passages and batches together, or nothing when a save fails, like the shared EF Core context.
/// </summary>
internal sealed class SyncBatchData
{
    public TelemetryIngestionData Telemetry { get; }
    public TrackingStartData Ready => Telemetry.Ready;
    public TrackingSession Session => Ready.Session!;
    public List<TelemetryRecord> StoredReadings => Telemetry.Stored;
    public List<CheckpointPassage> StoredPassages => Telemetry.Passages;
    public List<SyncBatch> StoredBatches { get; } = [];
    public List<CheckpointPassage> PendingPassages { get; } = [];
    public List<SyncBatch> PendingBatches { get; } = [];
    public int Saves { get; private set; }

    /// <summary>One-shot hook that runs when the next save starts; it may throw to simulate a storage failure.</summary>
    public Action? BeforeNextSave { get; set; }

    public ITrackingSessionRepository Sessions { get; }
    public ICheckpointPassageRepository PassageRepository { get; }
    public ISyncBatchRepository BatchRepository { get; }
    public IUnitOfWork UnitOfWork { get; }

    private SyncBatchData(TelemetryIngestionData telemetry)
    {
        Telemetry = telemetry;
        Sessions = InterfaceStub<ITrackingSessionRepository>.Create((method, args) => method.Name switch
        {
            nameof(ITrackingSessionRepository.FindByIdAndAgencyIdAsync) => Task.FromResult(
                Session.Id == (int)args[0]! && Session.AgencyId == (int)args[1]! ? Session : null),
            nameof(ITrackingSessionRepository.FindByIdAsync) => Task.FromResult(
                Session.Id == (int)args[0]! ? Session : null),
            _ => throw new InvalidOperationException(method.Name)
        });
        PassageRepository = InterfaceStub<ICheckpointPassageRepository>.Create((method, args) => method.Name switch
        {
            nameof(ICheckpointPassageRepository.FindBySessionIdAndCheckpointIdAsync) => Task.FromResult(
                PendingPassages.Concat(StoredPassages).FirstOrDefault(passage =>
                    passage.SessionId == (int)args[0]! && passage.CheckpointId == (int)args[1]!)),
            nameof(ICheckpointPassageRepository.AddAsync) => Add(PendingPassages, (CheckpointPassage)args[0]!),
            _ => throw new InvalidOperationException(method.Name)
        });
        BatchRepository = InterfaceStub<ISyncBatchRepository>.Create((method, args) => method.Name switch
        {
            nameof(ISyncBatchRepository.FindBySessionIdAndClientBatchIdAsync) => Task.FromResult(
                StoredBatches.FirstOrDefault(batch => batch.SessionId == (int)args[0]! &&
                    batch.ClientBatchId == (Guid)args[1]!)),
            nameof(ISyncBatchRepository.FindByIdAndAgencyIdAsync) => Task.FromResult(
                StoredBatches.FirstOrDefault(batch => batch.Id == (int)args[0]! && batch.AgencyId == (int)args[1]!)),
            nameof(ISyncBatchRepository.AddAsync) => Add(PendingBatches, (SyncBatch)args[0]!),
            _ => throw new InvalidOperationException(method.Name)
        });
        UnitOfWork = InterfaceStub<IUnitOfWork>.Create((method, _) =>
        {
            if (method.Name == nameof(IUnitOfWork.DiscardChanges))
            {
                Discard();
                return null;
            }

            if (method.Name != nameof(IUnitOfWork.CompleteAsync)) throw new InvalidOperationException(method.Name);
            var hook = BeforeNextSave;
            BeforeNextSave = null;
            hook?.Invoke();
            CommitPending();
            Saves++;
            return Task.CompletedTask;
        });
    }

    public static async Task<SyncBatchData> CreateAsync() => new(await TelemetryIngestionData.CreateAsync());

    /// <summary>Commits everything staged so far, as another request would.</summary>
    public void CommitPending()
    {
        Telemetry.UnitOfWork.CompleteAsync().GetAwaiter().GetResult();
        foreach (var passage in PendingPassages)
            StoredPassages.Add(TrackingStartData.WithId(passage, 700 + StoredPassages.Count + 1));
        foreach (var batch in PendingBatches)
            StoredBatches.Add(TrackingStartData.WithId(batch, 900 + StoredBatches.Count + 1));
        PendingPassages.Clear();
        PendingBatches.Clear();
    }

    private void Discard()
    {
        Telemetry.UnitOfWork.DiscardChanges();
        PendingPassages.Clear();
        PendingBatches.Clear();
    }

    private static Task Add<T>(List<T> pending, T entity)
    {
        pending.Add(entity);
        return Task.CompletedTask;
    }
}
