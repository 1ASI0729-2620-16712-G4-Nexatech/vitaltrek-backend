namespace VitalTrek.API.Shared.Domain.Model;

/// <summary>
///     Provides the operational calendar of the agencies, which operate in Peru (America/Lima, UTC-5).
/// </summary>
public static class AgencyCalendar
{
    private static readonly TimeZoneInfo OperationsTimeZone = ResolveTimeZone();

    /// <summary>
    ///     Gets the current local date of the operations time zone.
    /// </summary>
    /// <returns>The local date.</returns>
    public static DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, OperationsTimeZone).DateTime);

    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var id in new[] { "America/Lima", "SA Pacific Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var timeZone)) return timeZone;
        }
        return TimeZoneInfo.CreateCustomTimeZone("America/Lima", TimeSpan.FromHours(-5), "America/Lima", "America/Lima");
    }
}
