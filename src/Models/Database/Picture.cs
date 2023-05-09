using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Picture")]
public class Picture
{
    [Column]
    public long Timestamp { get; set; }

    [Column]
    public Guid ActivityId { get; set; }

    [Column]
    public string FileName { get; set; } = string.Empty;
}
