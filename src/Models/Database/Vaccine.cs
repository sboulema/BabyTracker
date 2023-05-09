using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Vaccine")]
public class Vaccine : IDbEntry
{
    [Column("VaccID")]
    public string VaccineId { get; set; } = string.Empty;

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public long Time { get; set; }

    public string BabyName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}
