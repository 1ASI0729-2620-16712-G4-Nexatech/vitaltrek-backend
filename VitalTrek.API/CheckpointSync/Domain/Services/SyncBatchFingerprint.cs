using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;

namespace VitalTrek.API.CheckpointSync.Domain.Services;

/// <summary>
///     Computes the content fingerprint of a sync batch. The fingerprint ignores the order of the records and the
///     server-resolved agency and field guide, so a retry of the same capture always produces the same value.
/// </summary>
public static class SyncBatchFingerprint
{
    /// <summary>Returns the lowercase SHA-256 hexadecimal fingerprint of the batch content.</summary>
    public static string Compute(SubmitSyncBatchCommand command)
    {
        var content = new StringBuilder("v1");
        Append(content, command.SessionId);
        Append(content, command.CheckpointId);
        foreach (var reading in command.Readings.OrderBy(reading => reading.ClientRecordId))
        {
            content.Append("|R");
            Append(content, reading.ClientRecordId.ToString("N"));
            Append(content, reading.WearableDeviceId);
            Append(content, Time(reading.RecordedAt));
            Append(content, Number(reading.Latitude));
            Append(content, Number(reading.Longitude));
            Append(content, Number(reading.Altitude));
            Append(content, reading.HeartRate);
            Append(content, reading.Battery);
            Append(content, Number(reading.DeviationMeters));
        }

        foreach (var passage in command.Passages.OrderBy(passage => passage.ClientRecordId))
        {
            content.Append("|P");
            Append(content, passage.ClientRecordId.ToString("N"));
            Append(content, passage.CheckpointId);
            Append(content, Time(passage.PassedAt));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content.ToString()))).ToLowerInvariant();
    }

    private static StringBuilder Append(StringBuilder content, object value) =>
        content.Append('|').Append(Convert.ToString(value, CultureInfo.InvariantCulture));

    // Same microsecond precision as the stored capture, independent of the offset sent by the client.
    private static long Time(DateTimeOffset value) => value.UtcTicks - value.UtcTicks % 10;

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
