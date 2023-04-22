using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("MedicineSelection")]
public class MedicationSelection
{
    [Column]
    public Guid Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column]
    public long Timestamp { get; set; }

    [Column]
    public string Description { get; set; } = string.Empty;

    [Column]
    public double AmountPerTime { get; set; }

    [Column]
    public string Unit { get; set; } = string.Empty;

    [Column]
    public int Interval { get; set; }
}
