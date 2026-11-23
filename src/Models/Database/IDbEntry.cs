using System;

namespace BabyTracker.Models.Database;

public interface IDbEntry
{
    Guid Id { get; set; }

    long Timestamp { get; set; }

    long Time { get; set; }

    string Note { get; set; }

    Guid BabyId { get; set; }
}
