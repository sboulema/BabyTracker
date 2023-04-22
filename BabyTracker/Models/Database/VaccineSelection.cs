using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("VaccineSelection")]
public class VaccineSelection
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
