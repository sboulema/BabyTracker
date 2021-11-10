using System;

namespace BabyTracker.Models;

public class BabyModel : EntryModel
{
    public DateTime DateOfBirth { get; set; }

    public DateTime DueDate { get; set; }

    public int Gender { get; set; }

    public string Filename { get; set; } = string.Empty;

    public string PictureFileName { get; set; } = string.Empty;
}
