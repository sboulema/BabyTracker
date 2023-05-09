using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("OtherFeed")]
public class Supplement : IDbEntry
{
    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public Guid BabyId { get; set; }

    [Column]
    public Guid TypeId { get; set; }

    [Column]
    public string Unit { get; set; } = string.Empty;

    [Column]
    public double Amount { get; set; }

    public string Title { get; set; } = string.Empty;
}
