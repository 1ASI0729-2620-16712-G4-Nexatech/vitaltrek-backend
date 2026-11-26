using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VitalTrek.API.CheckpointSync.Interfaces.Rest.Resources;

/// <summary>
///     Resource to submit the readings and passages captured offline. The agency and the field guide come from the
///     authenticated user; values sent for them in the body are ignored.
/// </summary>
public record SubmitSyncBatchResource(
    [property: JsonRequired] Guid ClientBatchId,
    [property: JsonRequired]
    [Range(1, int.MaxValue, ErrorMessage = "RequiredSessionId")] int SessionId,
    [property: JsonRequired]
    [Range(1, int.MaxValue, ErrorMessage = "RequiredCheckpointId")] int CheckpointId,
    [MaxLength(500, ErrorMessage = "SyncBatchTooLarge")] IReadOnlyList<SyncBatchReadingResource>? Readings,
    [MaxLength(500, ErrorMessage = "SyncBatchTooLarge")] IReadOnlyList<SyncBatchPassageResource>? Passages);

/// <summary>
///     A telemetry reading captured offline. The participant is resolved by the server from the wearable assignment.
/// </summary>
public record SyncBatchReadingResource(
    [property: JsonRequired] Guid ClientRecordId,
    [property: JsonRequired] int WearableDeviceId,
    [property: JsonRequired] DateTimeOffset RecordedAt,
    [property: JsonRequired] double Latitude,
    [property: JsonRequired] double Longitude,
    [property: JsonRequired] double Altitude,
    [property: JsonRequired] int HeartRate,
    [property: JsonRequired] int Battery,
    [property: JsonRequired] double DeviationMeters);

/// <summary>
///     A checkpoint passage captured offline.
/// </summary>
public record SyncBatchPassageResource(
    [property: JsonRequired] Guid ClientRecordId,
    [property: JsonRequired] int CheckpointId,
    [property: JsonRequired] DateTimeOffset PassedAt);
