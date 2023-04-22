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
}
