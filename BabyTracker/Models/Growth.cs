using CsvHelper.Configuration.Attributes;

namespace BabyTracker.Models
{
    public class Growth : EntryModel
    {
        public double Length { get; set; }

        public double Weight { get; set; }

        [Name("Head Size")]
        public double HeadSize { get; set; }
    }
}
