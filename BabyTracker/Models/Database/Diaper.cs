using BabyTracker.Constants;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Diaper")]
public class Diaper : IDbEntry
{
    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public DiaperStatusEnum Status { get; set; }
}
