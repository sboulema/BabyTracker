using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("OtherFeed")]
public class Supplement : IDbEntry
{
    [Column]
    public Guid Id { get; set; }

    [Column]
    public long Timestamp { get; set; }

    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public int? HasPicture { get; set; }

    [Column]
    public Guid BabyId { get; set; }

    [Column]
    public string TypeId { get; set; } = string.Empty;

    [Column]
    public string Unit { get; set; } = string.Empty;

    [Column]
    public double Amount { get; set; }

    public string Title { get; set; } = string.Empty;
}
