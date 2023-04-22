using System;
using LinqToDB.Mapping;

namespace BabyTracker.Models.Database;

[Table("OtherActivityDesc")]
public class ActivityDescription
{
    [Column]
    public Guid Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;
}
