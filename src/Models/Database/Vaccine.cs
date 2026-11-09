using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Vaccine")]
public class Vaccine : IDbEntry
{
    [Column]
    public Guid Id { get; set; }

    [Column]
    public long Timestamp { get; set; }

    [Column("VaccID")]
    public string VaccineId { get; set; } = string.Empty;

    [Column]
    public Guid BabyId { get; set; }

    [Column("DocVisitID")]
    public Guid DocVisitId { get; set; }

    [Column]
    public int? HasPicture { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public long Time { get; set; }

    public string BabyName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}
