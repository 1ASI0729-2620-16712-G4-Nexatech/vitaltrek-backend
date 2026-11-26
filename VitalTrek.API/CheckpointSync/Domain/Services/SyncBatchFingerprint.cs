using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using VitalTrek.API.CheckpointSync.Domain.Model.Commands;

namespace VitalTrek.API.CheckpointSync.Domain.Services;

/// <summary>
///     Computes content fingerprints of a sync batch and of its records. Fingerprints ignore the order of the records,
///     the offset used to send a timestamp and the server-resolved agency and field guide, so a retry of the same
///     capture always produces the same value.
/// </summary>
public static class SyncBatchFingerprint
{
    /// <summary>Returns the lowercase SHA-256 hexadecimal fingerprint of the whole batch content.</summary>
    public static string Compute(SubmitSyncBatchCommand command)
    {
        var content = new StringBuilder("v1");
        Append(content, command.SessionId);
        Append(content, command.CheckpointId);
        foreach (var reading in command.Readings.OrderBy(reading => reading.ClientRecordId))
            content.Append("|R").Append(Canonical(reading));
        foreach (var passage in command.Passages.OrderBy(passage => passage.ClientRecordId))
            content.Append("|P").Append(Canonical(passage));
        return Hash(content.ToString());
    }

    /// <summary>Returns the fingerprint of one reading, used to detect a reused record identity.</summary>
    public static string ForReading(SyncBatchReadingEntry reading) => Hash("R" + Canonical(reading));

    /// <summary>Returns the fingerprint of one passage, used to detect a reused record identity.</summary>
    public static string ForPassage(SyncBatchPassageEntry passage) => Hash("P" + Canonical(passage));

    private static string Canonical(SyncBatchReadingEntry reading)
    {
        var content = new StringBuilder();
        Append(content, reading.ClientRecordId.ToString("N"));
        Append(content, reading.WearableDeviceId);
        Append(content, Time(reading.RecordedAt));
        Append(content, Number(reading.Latitude));
        Append(content, Number(reading.Longitude));
        Append(content, Number(reading.Altitude));
        Append(content, reading.HeartRate);
        Append(content, reading.Battery);
        Append(content, Number(reading.DeviationMeters));
        return content.ToString();
    }

    private static string Canonical(SyncBatchPassageEntry passage)
    {
        var content = new StringBuilder();
        Append(content, passage.ClientRecordId.ToString("N"));
        Append(content, passage.CheckpointId);
        Append(content, Time(passage.PassedAt));
        return content.ToString();
    }

    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static void Append(StringBuilder content, object value) =>
        content.Append('|').Append(Convert.ToString(value, CultureInfo.InvariantCulture));

    // Same microsecond precision as the stored capture, independent of the offset sent by the client.
    private static long Time(DateTimeOffset value) => value.UtcTicks - value.UtcTicks % 10;

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
