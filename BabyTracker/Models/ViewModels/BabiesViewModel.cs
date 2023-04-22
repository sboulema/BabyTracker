using System.Collections.Generic;
using BabyTracker.Models.Database;

namespace BabyTracker.Models.ViewModels;

public class BabiesViewModel : BaseViewModel
{
    public List<Baby> Babies { get; set; } = new();
}
