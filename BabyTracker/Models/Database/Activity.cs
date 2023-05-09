using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("OtherActivity")]
public class Activity : IDbEntry, IMemoryEntry
{
    [Column]
    public Guid Id { get; set; }

    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column("DescID")]
    public Guid DescriptionId { get; set; }

    [Column]
    public Guid BabyId { get; set; }

    [Column]
    public int Duration { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column]
    public string FileName { get; set; } = string.Empty;
}
