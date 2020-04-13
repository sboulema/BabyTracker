using CsvHelper.Configuration.Attributes;

namespace BabyTracker.Models
{
    public class Growth : EntryModel
    {
        public string Length { get; set; }

        public string Weight { get; set; }

        [Name("Head Size")]
        public string HeadSize { get; set; }
    }
}
