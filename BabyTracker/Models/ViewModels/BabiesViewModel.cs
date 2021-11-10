using System.Collections.Generic;

namespace BabyTracker.Models.ViewModels;

public class BabiesViewModel : BaseViewModel
{
    public List<EntryModel> Babies { get; set; } = new();
}
