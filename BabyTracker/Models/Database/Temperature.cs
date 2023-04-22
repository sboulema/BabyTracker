using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Temperature")]
public class Temperature : IDbEntry
{
    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public Guid BabyId { get; set; }

    [Column]
    public double Temp { get; set; }
}
