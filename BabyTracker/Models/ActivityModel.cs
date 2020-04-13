using CsvHelper.Configuration.Attributes;

namespace BabyTracker.Models
{
    public class ActivityModel : EntryModel
    {
        [Name("Other activity")]
        public string OtherActivity { get; set; }

        public string Duration { get; set; }

        public string Filename { get; set; }
    }
}
