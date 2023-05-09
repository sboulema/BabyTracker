namespace BabyTracker.Models.Database;

public interface IMemoryEntry
{
    long Time { get; set; }

    string Note { get; set; }

    string FileName { get; set; }
}
