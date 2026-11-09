using System;
using TimeZoneConverter;

namespace BabyTracker.Extensions;

public static class TimeExtensions
{
    public static DateTime ToDateTimeUTC(this long timestamp)
        => DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

    public static DateTime ToDateTimeLocal(this long timestamp)
        => TimeZoneInfo.ConvertTimeFromUtc(timestamp.ToDateTimeUTC(), TZConvert.GetTimeZoneInfo("W. Europe Standard Time"));

    public static DateOnly ToDateOnly(this long timestamp)
        => DateOnly.FromDateTime(timestamp.ToDateTimeUTC());

    public static long ToUnixTimestamp(this DateTime dateTime)
        => new DateTimeOffset(dateTime).ToUnixTimeSeconds();

    /// <summary>
    /// The offset from UTC of the time zone of the data clone without daylight saving time, in seconds,
    /// like the app's TimeZone.getRawOffset() / 1000.
    /// </summary>
    public static int GetCloneTimeZoneRawOffsetSeconds()
        => (int)TZConvert.GetTimeZoneInfo("W. Europe Standard Time").BaseUtcOffset.TotalSeconds;

    /// <summary>
    /// Converts a wall-clock time in the time zone of the data clone (the inverse of
    /// <see cref="ToDateTimeLocal"/>) to a Unix timestamp, independent of the server's time zone.
    /// </summary>
    public static long ToUnixTimestampFromLocal(this DateTime localDateTime)
    {
        var timeZone = TZConvert.GetTimeZoneInfo("W. Europe Standard Time");
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        // Skip over the hour that does not exist when the clocks go forward
        if (timeZone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone)).ToUnixTimeSeconds();
    }
}
