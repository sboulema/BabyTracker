using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Formula")]
public class Formula : IDbEntry
{
    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public long Amount { get; set; }
}
