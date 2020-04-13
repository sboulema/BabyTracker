using CsvHelper.Configuration.Attributes;

namespace BabyTracker.Models
{
    public class MedicationModel : EntryModel
    {
        [Name("Medication name")]
        public string MedicationName { get; set; }

        public string Amount { get; set; }
    }
}
