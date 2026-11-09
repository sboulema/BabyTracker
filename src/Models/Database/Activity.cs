using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("OtherActivity")]
public class Activity : IDbEntry, IMemoryEntry
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

    [Column("DescID")]
    public string DescriptionId { get; set; } = string.Empty;

    [Column]
    public Guid BabyId { get; set; }

    [Column]
    public int Duration { get; set; }

    // Not columns of OtherActivity: filled from the description and picture joins when reading.
    [Column(SkipOnInsert = true, SkipOnUpdate = true)]
    public string Name { get; set; } = string.Empty;

    [Column(SkipOnInsert = true, SkipOnUpdate = true)]
    public string FileName { get; set; } = string.Empty;
}
