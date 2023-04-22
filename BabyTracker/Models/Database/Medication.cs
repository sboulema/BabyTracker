using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("Medicine")]
public class Medication : IDbEntry
{
    [Column]
    public long Time { get; set; }

    [Column]
    public string Note { get; set; } = string.Empty;

    [Column]
    public Guid BabyId { get; set; }

    [Column("MedID")]
    public Guid MedicineSelectionId { get; set; }

    [Column]
    public int Amount { get; set; }

    public string MedicationName { get; set; } = string.Empty;

    public double AmountPerTime { get; set; }

    public string Unit { get; set; } = string.Empty;
}
