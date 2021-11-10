using System.Collections.Generic;

namespace BabyTracker.Models;

public class ImportResultModel
{
    public List<EntryModel> Entries { get; set; } = new();
}
