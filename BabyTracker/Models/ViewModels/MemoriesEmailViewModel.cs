using System.Linq;
using BabyTracker.Models.Database;

namespace BabyTracker.Models.ViewModels;

public class MemoriesEmailViewModel
{
    public string BaseUrl { get; set; } = string.Empty;

    public string BabyName { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public IOrderedEnumerable<IGrouping<int, IDbEntry>> Entries { get; set; }
}
