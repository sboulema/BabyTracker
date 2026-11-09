using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Formula")]
public class Formula : IDbEntry
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
    public long Amount { get; set; }

    [Column]
    public int IsEnglishScale { get; set; }
}
