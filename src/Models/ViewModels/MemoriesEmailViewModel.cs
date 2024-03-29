﻿using System.Collections.Generic;
using System.Linq;
using BabyTracker.Models.Database;

namespace BabyTracker.Models.ViewModels;

public class MemoriesEmailViewModel
{
    public string BaseUrl { get; set; } = string.Empty;

    public string BabyName { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public IEnumerable<IGrouping<int, IMemoryEntry>> Entries { get; set; } = Enumerable.Empty<IGrouping<int, IMemoryEntry>>();
}
