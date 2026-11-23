using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("OtherActivityDesc")]
public class ActivityDescription
{
    [Column]
    public string Id { get; set; } = string.Empty;

    [Column]
    public string Name { get; set; } = string.Empty;
}
