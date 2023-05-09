using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Sleep")]
public class Sleep : IDbEntry
{
    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public Guid BabyId { get; set; }

    [Column]
    public int Duration { get; set; }

    public string DurationText {
        get 
        {
            if (Duration < 60)
            {
                return $"{Duration} min";
            }

            return $"{Duration / 60} hrs {Duration % 60} min";
        }
    }
}
