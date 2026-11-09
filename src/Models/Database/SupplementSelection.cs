using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("OtherFeedSelection")]
public class SupplementSelection
{
    [Column]
    public string Id { get; set; } = string.Empty;

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column]
    public long Timestamp { get; set; }

    [Column]
    public string Description { get; set; } = string.Empty;
}
