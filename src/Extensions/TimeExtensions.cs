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
