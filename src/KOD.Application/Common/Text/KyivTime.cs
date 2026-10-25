namespace KOD.Application.Common.Text;

/// <summary>
/// The time zone of the cabinet. Dates entered or shown in the interface are Kyiv dates; moments are stored in UTC.
/// </summary>
public static class KyivTime
{
    /// <summary>
    /// Gets the Kyiv time zone (with daylight saving time); UTC when the OS has no time zone data.
    /// </summary>
    public static TimeZoneInfo Zone { get; } = Resolve();

    /// <summary>
    /// Gets the moment a Kyiv calendar day starts (00:00 local time), taking daylight saving time into account.
    /// </summary>
    /// <param name="date">The Kyiv date.</param>
    /// <returns>The moment in UTC.</returns>
    public static DateTimeOffset StartOfDay(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }

    /// <summary>
    /// Finds the Kyiv time zone on any OS (IANA and Windows identifiers).
    /// </summary>
    /// <returns>The time zone.</returns>
    private static TimeZoneInfo Resolve()
    {
        foreach (var id in (string[])["Europe/Kyiv", "Europe/Kiev", "FLE Standard Time"])
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
            {
                return zone;
            }
        }

        return TimeZoneInfo.Utc;
    }
}
