namespace BabyTracker.Models
{
    public class MedicationModel : EntryModel
    {
        public string MedicationName { get; set; }

        public int Amount { get; set; }

        public int AmountPerTime { get; set; }

        public string Unit { get; set; }
    }
}
