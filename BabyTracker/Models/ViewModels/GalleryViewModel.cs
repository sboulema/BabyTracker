using System.Collections.Generic;

namespace BabyTracker.Models.ViewModels;

public class GalleryViewModel : BaseViewModel
{
    public List<PictureModel> Pictures { get; set; } = new();
}
