using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Medicine")]
public class Medication : IDbEntry
{
    [Column]
    public Guid Id { get; set; }

    [Column]
    public long Timestamp { get; set; }

    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public int? HasPicture { get; set; }

    [Column]
    public Guid BabyId { get; set; }

    [Column("MedID")]
    public Guid MedicineSelectionId { get; set; }

    [Column]
    public double Amount { get; set; }

    public string MedicationName { get; set; } = string.Empty;

    public double AmountPerTime { get; set; }

    public string Unit { get; set; } = string.Empty;
}
