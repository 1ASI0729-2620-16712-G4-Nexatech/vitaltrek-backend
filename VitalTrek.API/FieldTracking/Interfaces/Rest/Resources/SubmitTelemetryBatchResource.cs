using System.ComponentModel.DataAnnotations;

namespace VitalTrek.API.FieldTracking.Interfaces.Rest.Resources;

/// <summary>
///     Resource to submit a batch of GPS measurements for a wearable device.
/// </summary>
public record SubmitTelemetryBatchResource(
    [Required(ErrorMessage = "RequiredWearableDeviceId")] int WearableDeviceId,
    [Required(ErrorMessage = "RequiredRecords"), MinLength(1, ErrorMessage = "BatchMustHaveRecords")]
    IReadOnlyList<TelemetryRecordEntryResource> Records);

/// <summary>
///     A single GPS measurement within the batch payload.
/// </summary>
public record TelemetryRecordEntryResource(
    [Range(-90, 90, ErrorMessage = "InvalidLatitude")] double Latitude,
    [Range(-180, 180, ErrorMessage = "InvalidLongitude")] double Longitude,
    double Altitude,
    DateTimeOffset RecordedAt);
