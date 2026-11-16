using System;
using BabyTracker.Constants;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Diaper")]
public class Diaper : IDbEntry
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
    public DiaperStatusEnum Status { get; set; }
}
