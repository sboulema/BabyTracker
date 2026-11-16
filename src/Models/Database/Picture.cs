using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Picture")]
public class Picture
{
    [Column]
    public Guid Id { get; set; }

    [Column]
    public long Timestamp { get; set; }

    /// <summary>The id of the entry (for example a Joy) this picture belongs to.</summary>
    [Column]
    public Guid ActivityId { get; set; }

    [Column]
    public int? Type { get; set; }

    [Column]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Only filled when inserting. The gallery query projects the other columns, so the BLOBs are not loaded.
    /// </summary>
    [Column(SkipOnUpdate = true)]
    public byte[]? Thumbnail { get; set; }
}
