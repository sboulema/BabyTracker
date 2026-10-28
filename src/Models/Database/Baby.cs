using System;
using LinqToDB.Mapping;
using NodaTime;

namespace BabyTracker.Models.Database;

[Table("Baby")]
public class Baby
{
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

    public string Age()
    {
        var birthday = Instant.FromUnixTimeSeconds(DOB).InUtc().Date;
        var today = LocalDate.FromDateTime(DateTime.UtcNow);
        var period = Period.Between(birthday, today,
                            PeriodUnits.Months | PeriodUnits.Days);
        return $"{period.Months} months {period.Days} days";
    }

    public int AgeInMonths()
    {
        var birthday = Instant.FromUnixTimeSeconds(DOB).InUtc().Date;
        var today = LocalDate.FromDateTime(DateTime.UtcNow);
        var period = Period.Between(birthday, today, PeriodUnits.Months);
        return period.Months;
    }

    public double AgeInMonthsAt(Instant moment)
    {
        var birthday = Instant.FromUnixTimeSeconds(DOB).InUtc().Date;
        var date = moment.InUtc().Date;

        var wholeMonths = Period.Between(birthday, date, PeriodUnits.Months).Months;
        var monthStart = birthday.PlusMonths(wholeMonths);
        var monthEnd = birthday.PlusMonths(wholeMonths + 1);

        return wholeMonths
            + (double)Period.DaysBetween(monthStart, date) / Period.DaysBetween(monthStart, monthEnd);
    }
}
