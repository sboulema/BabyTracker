namespace BabyTracker.Models;

public class MedicationModel : EntryModel
{
    public string MedicationName { get; set; } = string.Empty;

    public int Amount { get; set; }

    public int AmountPerTime { get; set; }

    public string Unit { get; set; } = string.Empty;
}
