using System.Collections.Generic;
using BabyTracker.Models.Database;

namespace BabyTracker.Models.ViewModels;

public class GalleryViewModel : BaseViewModel
{
    public List<Picture> Pictures { get; set; } = new();
}
