using BabyTracker.Models.Database;
using BabyTracker.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace BabyTracker.Services;

public static class DiaryService
{
    public static DiaryViewModel GetDays(List<IDbEntry> entries)
        => new()
        {
            Entries = entries.OrderByDescending(e => e.Time).Select(Map)
        };

    public static DiaryViewModel GetDays(List<IMemoryEntry> entries)
        => GetDays(entries.Cast<IDbEntry>().ToList());

    private static DiaryEntryViewModel Map(IDbEntry entry)
    {
        if (entry is Diaper diaper)
        {
            return new()
            {
                CssClass = "diaper",
                CssColor = "blue",
                IconURL = "/img/icon_review_diaper.png",
                Note = diaper.Note,
                TimeStamp = diaper.Time,
                Title = diaper.Status.ToString()
            };
        }
        else if (entry is Formula formula)
        {
            return new()
            {
                CssClass = "formula",
                CssColor = "orange",
                IconURL = "/img/icon_review_formula.png",
                Note = formula.Note,
                TimeStamp = formula.Time,
                Title = $"Formula {formula.Amount} ml"
            };
        }
        else if (entry is Joy joy)
        {
            return new()
            {
                CssClass = "joy",
                CssColor = "green",
                IconURL = "/img/icon_review_joy.png",
                Note = joy.Note,
                PictureFileName = joy.FileName,
                TimeStamp = joy.Time,
                Title = "Joy"
            };
        }
        else if (entry is Growth growth)
        {
            return new()
            {
                CssClass = "growth",
                CssColor = "green",
                IconURL = "/img/icon_review_growth.png",
                Note = growth.Note,
                TimeStamp = growth.Time,
                Title = $"Growth Length: {growth.Length:n2} Weight: {growth.Weight:n2} Head: {growth.HeadSize:n2}"
            };
        }
        else if (entry is Medication medication)
        {
            return new()
            {
                CssClass = "medication",
                CssColor = "green",
                IconURL = "/img/icon_review_medication.png",
                Note = medication.Note,
                TimeStamp = medication.Time,
                Title = $"{medication.MedicationName} {medication.AmountPerTime} {medication.Unit}"
            };
        }
        else if (entry is Milestone milestone)
        {
            return new()
            {
                CssClass = "milestone",
                CssColor = "green",
                IconURL = "/img/icon_review_milestone.png",
                Note = milestone.Note,
                PictureFileName = milestone.FileName,
                TimeStamp = milestone.Time,
                Title = milestone.Name
            };
        }
        else if (entry is Activity activity)
        {
            return new()
            {
                CssClass = "activity",
                CssColor = "green",
                IconURL = "/img/icon_review_activity.png",
                Note = activity.Note,
                PictureFileName = activity.FileName,
                TimeStamp = activity.Time,
                Title = activity.Name
            };
        }
        else if (entry is Sleep sleep)
        {
            return new()
            {
                CssClass = "sleep",
                CssColor = "dark-green",
                IconURL = "/img/icon_review_sleep.png",
                Note = sleep.Note,
                TimeStamp = sleep.Time,
                Title = sleep.Duration == 0 ? "Started sleeping" : $"Slept {sleep.DurationText}"
            };
        }
        else if (entry is Supplement supplement)
        {
            return new()
            {
                CssClass = "supplement",
                CssColor = "orange",
                IconURL = "/img/icon_review_supplement.png",
                Note = supplement.Note,
                TimeStamp = supplement.Time,
                Title = supplement.Title
            };
        }
        else if (entry is Temperature temperature)
        {
            return new()
            {
                CssClass = "temperature",
                CssColor = "green",
                IconURL = "/img/icon_review_temperature.png",
                Note = temperature.Note,
                TimeStamp = temperature.Time,
                Title = $"Temperature {temperature.Temp:n1} &deg;C"
            };
        }
        else if (entry is Vaccine vaccine)
        {
            return new()
            {
                CssClass = "vaccine",
                CssColor = "dark-green",
                IconURL = "/img/icon_review_vaccine.png",
                Note = vaccine.Note,
                TimeStamp = vaccine.Time,
                Title = vaccine.Title
            };
        }

        return new();
    }
}
