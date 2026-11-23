using BabyTracker.Constants;
using BabyTracker.Models.Database;
using BabyTracker.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BabyTracker.Services;

public static class DiaryService
{
    /// <param name="babyName">The baby the entries belong to. Needed for the entries to get a link to edit them.</param>
    public static DiaryViewModel GetDays(List<IDbEntry> entries, string? babyName = null)
        => new()
        {
            Entries = entries.OrderByDescending(e => e.Time).Select(entry => Map(entry, babyName))
        };

    public static DiaryViewModel GetDays(List<IMemoryEntry> entries)
        => GetDays(entries.Cast<IDbEntry>().ToList());

    private static DiaryEntryViewModel Map(IDbEntry entry, string? babyName)
    {
        var model = MapEntry(entry);

        // Entries of the memories do not have an id, and have no baby name: those cannot be edited
        if (model.Type is not null && model.Id != Guid.Empty && !string.IsNullOrEmpty(babyName))
        {
            model.EditUrl = $"/{Uri.EscapeDataString(babyName)}/edit/{model.Type}/{model.Id:D}";
        }

        return model;
    }

    private static DiaryEntryViewModel MapEntry(IDbEntry entry)
    {
        if (entry is Diaper diaper)
        {
            return new()
            {
                Id = diaper.Id,
                Type = EntryTypeEnum.Diaper,
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
                Id = formula.Id,
                Type = EntryTypeEnum.Formula,
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
                Id = joy.Id,
                Type = EntryTypeEnum.Joy,
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
                Id = growth.Id,
                Type = EntryTypeEnum.Growth,
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
                Id = medication.Id,
                Type = EntryTypeEnum.Medication,
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
                Id = milestone.Id,
                Type = EntryTypeEnum.Milestone,
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
                Id = activity.Id,
                Type = EntryTypeEnum.Activity,
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
                Id = sleep.Id,
                Type = EntryTypeEnum.Sleep,
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
                Id = supplement.Id,
                Type = EntryTypeEnum.Supplement,
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
                Id = temperature.Id,
                Type = EntryTypeEnum.Temperature,
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
                Id = vaccine.Id,
                Type = EntryTypeEnum.Vaccine,
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
