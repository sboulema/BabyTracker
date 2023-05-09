using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Growth")]
public class Growth : IDbEntry
{
    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public Guid BabyId { get; set; }

    [Column]
    public double Length { get; set; }

    [Column]
    public double Weight { get; set; }

    [Column("Head")]
    public double HeadSize { get; set; }
}
