using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Milestone")]
public class Milestone : IDbEntry, IMemoryEntry
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
    public string MilestoneSelectionId { get; set; } = string.Empty;

    // Not columns of Milestone: filled from the selection and picture joins when reading.
    [Column(SkipOnInsert = true, SkipOnUpdate = true)]
    public string Name { get; set; } = string.Empty;

    [Column(SkipOnInsert = true, SkipOnUpdate = true)]
    public string FileName { get; set; } = string.Empty;
}
