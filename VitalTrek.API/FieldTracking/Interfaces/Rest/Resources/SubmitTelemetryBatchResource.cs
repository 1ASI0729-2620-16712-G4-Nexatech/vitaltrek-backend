using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;

/// <summary>
///     Resource to submit a batch of GPS measurements for a wearable device.
/// </summary>
public record SubmitTelemetryBatchResource(
    [Required(ErrorMessage = "RequiredWearableDeviceId")] int WearableDeviceId,
    [Required(ErrorMessage = "RequiredRecords"), MinLength(1, ErrorMessage = "BatchMustHaveRecords"),
     MaxLength(500, ErrorMessage = "TelemetryBatchTooLarge")]
    IReadOnlyList<TelemetryRecordEntryResource> Records);

/// <summary>
///     A single GPS measurement within the batch payload.
/// </summary>
public record TelemetryRecordEntryResource(
    [property: JsonRequired]
    [Range(-90, 90, ErrorMessage = "InvalidLatitude")] double Latitude,
    [property: JsonRequired]
    [Range(-180, 180, ErrorMessage = "InvalidLongitude")] double Longitude,
    [property: JsonRequired]
    double Altitude,
    [property: JsonRequired]
    DateTimeOffset RecordedAt,
    [property: JsonRequired]
    Guid ClientRecordId,
    [property: JsonRequired]
    int HeartRate,
    [property: JsonRequired]
    int Battery,
    [property: JsonRequired]
    double DeviationMeters);
