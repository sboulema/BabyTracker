using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Milestone")]
public class Milestone : IDbEntry, IMemoryEntry
{
    [Column]
    public Guid Id { get; set; }

    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public Guid BabyId { get; set; }

    [Column]
    public Guid MilestoneSelectionId { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column]
    public string FileName { get; set; } = string.Empty;
}
