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
}
