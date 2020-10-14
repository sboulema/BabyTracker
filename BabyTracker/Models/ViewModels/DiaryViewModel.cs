using System;
using System.Collections.Generic;
using System.Linq;

namespace BabyTracker.Models.ViewModels
{
    public class DiaryViewModel
    {
        public IEnumerable<IGrouping<DateTime, EntryModel>> Days { get; set; }

        public IEnumerable<EntryModel> Entries { get; set; }

        public string PictureDirectory { get; set; }

        public List<string> EntryTypes { get; set; } = new List<string> { "Diaper", "Formula", "Supplement", "Joy", "Growth", "Medication", "Milestone", "Activity", "Sleep", "Temperature", "Vaccine" };

        public string Date { get; set; }

        public string DateNext { get; set; }

        public string DatePrevious { get; set; }
    }
}
