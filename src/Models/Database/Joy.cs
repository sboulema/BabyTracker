using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Joy")]
public class Joy : IDbEntry, IMemoryEntry
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

    // Not a column of Joy: filled from the Picture join when reading.
    [Column(SkipOnInsert = true, SkipOnUpdate = true)]
    public string FileName { get; set; } = string.Empty;
}
