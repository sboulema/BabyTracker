using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

/// <summary>
/// A doctor visit. Only the columns needed to create one are mapped: every vaccine
/// belongs to a visit (Vaccine.DocVisitID is NOT NULL).
/// </summary>
[Table("DoctorVisit")]
public class DoctorVisit
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

    [Column]
    public int IsAppointment { get; set; }
}
