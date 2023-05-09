using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Joy")]
public class Joy : IDbEntry, IMemoryEntry
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
    public string FileName { get; set; } = string.Empty;
}
