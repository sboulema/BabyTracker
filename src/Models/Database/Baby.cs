using System;
using BabyTracker.Extensions;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Baby")]
public class Baby
{
    [Column]
    public Guid Id { get; set; }

    [Column]
    public long Timestamp { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column]
    public long DOB { get; set; }

    [Column("DueDay")]
    public long DueDate { get; set; }

    [Column]
    public int Gender { get; set; }

    [Column("Picture")]
    public string PictureFileName { get; set; } = string.Empty;

    private DateOnly Birthday
        => DOB.ToDateOnly();

    private static DateOnly Today
        => DateOnly.FromDateTime(DateTime.UtcNow);

    public string Age()
    {
        var (months, days) = Birthday.MonthsAndDaysUntil(Today);
        return $"{months} months {days} days";
    }

    public int AgeInMonths()
        => Birthday.MonthsAndDaysUntil(Today).Months;

    public double AgeInMonthsAt(long timestamp)
    {
        var date = timestamp.ToDateOnly();
        var wholeMonths = Birthday.MonthsAndDaysUntil(date).Months;
        var monthStart = Birthday.AddMonths(wholeMonths);
        var monthEnd = Birthday.AddMonths(wholeMonths + 1);

        return wholeMonths
            + ((double)(date.DayNumber - monthStart.DayNumber) / (monthEnd.DayNumber - monthStart.DayNumber));
    }
}
