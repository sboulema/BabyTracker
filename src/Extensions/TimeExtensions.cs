using System;

namespace BabyTracker.Extensions;

public static class TimeExtensions
{
    public static DateTime ToDateTimeUTC(this long timestamp)
        => DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

    public static DateTime ToDateTimeLocal(this long timestamp)
        => timestamp.ToDateTimeUTC().ToLocalTime();

    public static DateOnly ToDateOnly(this long timestamp)
        => DateOnly.FromDateTime(timestamp.ToDateTimeUTC());

    public static long ToUnixTimestamp(this DateTime dateTime)
        => new DateTimeOffset(dateTime).ToUnixTimeSeconds();

    /// <summary>
    /// The offset from UTC of the time zone of the data clone without daylight saving time, in seconds,
    /// like the app's TimeZone.getRawOffset() / 1000.
    /// </summary>
    public static int GetCloneTimeZoneRawOffsetSeconds()
        => (int)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds;

    /// <summary>
    /// Converts a wall-clock time in the time zone of the data clone (the inverse of
    /// <see cref="ToDateTimeLocal"/>) to a Unix timestamp, independent of the server's time zone.
    /// </summary>
    public static long ToUnixTimestampFromLocal(this DateTime localDateTime)
    {
        var timeZone = TimeZoneInfo.Local;
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        // Skip over the hour that does not exist when the clocks go forward
        if (timeZone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone)).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Whole months plus remaining days between two dates.
    /// </summary>
    public static (int Months, int Days) MonthsAndDaysUntil(this DateOnly start, DateOnly end)
    {
        var months = ((end.Year - start.Year) * 12) + end.Month - start.Month;

        if (start.AddMonths(months) > end)
        {
            months--;
        }

        var days = end.DayNumber - start.AddMonths(months).DayNumber;

        return (months, days);
    }
}
