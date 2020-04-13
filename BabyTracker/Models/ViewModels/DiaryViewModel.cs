using System;
using System.Collections.Generic;
using System.Linq;

namespace BabyTracker.Models.ViewModels
{
    public class DiaryViewModel
    {
        public IEnumerable<IGrouping<DateTime, EntryModel>> Days { get; set; }
    }
}
