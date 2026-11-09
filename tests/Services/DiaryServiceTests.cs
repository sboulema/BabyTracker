using System;
using System.Collections.Generic;
using BabyTracker.Constants;
using BabyTracker.Models.Database;
using BabyTracker.Models.ViewModels;
using BabyTracker.Services;
using NUnit.Framework;

namespace BabyTracker.Tests.Services;

[TestFixture]
public class DiaryServiceTests
{
    [Test]
    public void GetDays_WithEmptyList_ReturnsEmptyViewModel()
    {
        var entries = new List<IDbEntry>();
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Is.Empty);
    }

    [Test]
    public void GetDays_WithDiaperEntry_ReturnsCorrectViewModel()
    {
        var diaper = new Diaper
        {
            Time = 1705320000,
            Note = "Test note",
            Status = DiaperStatusEnum.Wet
        };
        var entries = new List<IDbEntry> { diaper };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("diaper"));
        Assert.That(entry.CssColor, Is.EqualTo("blue"));
        Assert.That(entry.IconURL, Is.EqualTo("/img/icon_review_diaper.png"));
        Assert.That(entry.Note, Is.EqualTo("Test note"));
        Assert.That(entry.TimeStamp, Is.EqualTo(1705320000));
        Assert.That(entry.Title, Is.EqualTo("Wet"));
    }

    [Test]
    public void GetDays_WithFormulaEntry_ReturnsCorrectViewModel()
    {
        var formula = new Formula
        {
            Time = 1705320000,
            Note = "Test formula",
            Amount = 250
        };
        var entries = new List<IDbEntry> { formula };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("formula"));
        Assert.That(entry.CssColor, Is.EqualTo("orange"));
        Assert.That(entry.IconURL, Is.EqualTo("/img/icon_review_formula.png"));
        Assert.That(entry.Title, Is.EqualTo("Formula 250 ml"));
    }

    [Test]
    public void GetDays_WithJoyEntry_ReturnsCorrectViewModel()
    {
        var joy = new Joy
        {
            Time = 1705320000,
            Note = "Happy moment",
            FileName = "joy123.jpg"
        };
        var entries = new List<IDbEntry> { joy };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("joy"));
        Assert.That(entry.CssColor, Is.EqualTo("green"));
        Assert.That(entry.IconURL, Is.EqualTo("/img/icon_review_joy.png"));
        Assert.That(entry.Title, Is.EqualTo("Joy"));
        Assert.That(entry.PictureFileName, Is.EqualTo("joy123.jpg"));
    }

    [Test]
    public void GetDays_WithGrowthEntry_ReturnsCorrectViewModel()
    {
        var growth = new Growth
        {
            Time = 1705320000,
            Note = "Monthly check",
            Length = 75.5m,
            Weight = 10.2m,
            HeadSize = 45.3m
        };
        var entries = new List<IDbEntry> { growth };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("growth"));
        Assert.That(entry.Title, Is.EqualTo("Growth Length: 75.50 Weight: 10.20 Head: 45.30"));
    }

    [Test]
    public void GetDays_WithSleepEntryWithDuration_ReturnsCorrectViewModel()
    {
        var sleep = new Sleep
        {
            Time = 1705320000,
            Note = "Nap time",
            Duration = 90
        };
        var entries = new List<IDbEntry> { sleep };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("sleep"));
        Assert.That(entry.CssColor, Is.EqualTo("dark-green"));
        Assert.That(entry.Title, Is.EqualTo("Slept 1 hrs 30 min"));
    }

    [Test]
    public void GetDays_WithSleepEntryWithoutDuration_ReturnsCorrectViewModel()
    {
        var sleep = new Sleep
        {
            Time = 1705320000,
            Note = "Nap time",
            Duration = 0
        };
        var entries = new List<IDbEntry> { sleep };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.Title, Is.EqualTo("Started sleeping"));
    }

    [Test]
    public void GetDays_WithTemperatureEntry_ReturnsCorrectViewModel()
    {
        var temperature = new Temperature
        {
            Time = 1705320000,
            Note = "Fever check",
            Temp = 37.5m
        };
        var entries = new List<IDbEntry> { temperature };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("temperature"));
        Assert.That(entry.Title, Is.EqualTo("Temperature 37.5 &deg;C"));
    }

    [Test]
    public void GetDays_WithMedicationEntry_ReturnsCorrectViewModel()
    {
        var medication = new Medication
        {
            Time = 1705320000,
            Note = "Pain relief",
            MedicationName = "Paracetamol",
            AmountPerTime = 5,
            Unit = "ml"
        };
        var entries = new List<IDbEntry> { medication };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("medication"));
        Assert.That(entry.Title, Is.EqualTo("Paracetamol 5 ml"));
    }

    [Test]
    public void GetDays_WithActivityEntry_ReturnsCorrectViewModel()
    {
        var activity = new Activity
        {
            Time = 1705320000,
            Note = "Play time",
            Name = "Tummy time",
            FileName = "activity123.jpg"
        };
        var entries = new List<IDbEntry> { activity };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("activity"));
        Assert.That(entry.Title, Is.EqualTo("Tummy time"));
        Assert.That(entry.PictureFileName, Is.EqualTo("activity123.jpg"));
    }

    [Test]
    public void GetDays_WithSupplementEntry_ReturnsCorrectViewModel()
    {
        var supplement = new Supplement
        {
            Time = 1705320000,
            Note = "Vitamin D",
            Title = "Vitamin D drops"
        };
        var entries = new List<IDbEntry> { supplement };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("supplement"));
        Assert.That(entry.CssColor, Is.EqualTo("orange"));
        Assert.That(entry.Title, Is.EqualTo("Vitamin D drops"));
    }

    [Test]
    public void GetDays_WithVaccineEntry_ReturnsCorrectViewModel()
    {
        var vaccine = new Vaccine
        {
            Time = 1705320000,
            Note = "First dose",
            Title = "MMR"
        };
        var entries = new List<IDbEntry> { vaccine };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("vaccine"));
        Assert.That(entry.CssColor, Is.EqualTo("dark-green"));
        Assert.That(entry.Title, Is.EqualTo("MMR"));
    }

    [Test]
    public void GetDays_WithMilestoneEntry_ReturnsCorrectViewModel()
    {
        var milestone = new Milestone
        {
            Time = 1705320000,
            Note = "First steps",
            Name = "First Steps",
            FileName = "milestone123.jpg"
        };
        var entries = new List<IDbEntry> { milestone };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        
        var entriesList = result.Entries.ToList();
        DiaryEntryViewModel entry = entriesList[0];
        Assert.That(entry.CssClass, Is.EqualTo("milestone"));
        Assert.That(entry.Title, Is.EqualTo("First Steps"));
        Assert.That(entry.PictureFileName, Is.EqualTo("milestone123.jpg"));
    }

    [Test]
    public void GetDays_WithMultipleEntries_ReturnsAllEntries()
    {
        var entries = new List<IDbEntry>
        {
            new Diaper { Time = 1705320000, Status = DiaperStatusEnum.Wet },
            new Formula { Time = 1705320001, Amount = 200 },
            new Joy { Time = 1705320002, FileName = "joy.jpg" }
        };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(3));
    }

    [Test]
    public void GetDays_WithIMemoryEntry_ReturnsCorrectViewModel()
    {
        var joy = new Joy
        {
            Time = 1705320000,
            Note = "Memory",
            FileName = "memory123.jpg"
        };
        var entries = new List<IMemoryEntry> { joy };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        Assert.That(result.Entries.ToList()[0].PictureFileName, Is.EqualTo("memory123.jpg"));
    }

    [Test]
    public void GetDays_WithUnknownEntryType_ReturnsEmptyViewModel()
    {
        // Create a mock entry that doesn't match any known type
        var entries = new List<IDbEntry> { new TestUnknownEntry() };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(1));
        // Unknown entries should return an empty DiaryEntryViewModel
        Assert.That(result.Entries.ToList()[0].Title, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetDays_OrdersEntriesByTimeDescending()
    {
        var entries = new List<IDbEntry>
        {
            new Diaper { Time = 1705320000, Status = DiaperStatusEnum.Wet },
            new Diaper { Time = 1705320002, Status = DiaperStatusEnum.Dirty },
            new Diaper { Time = 1705320001, Status = DiaperStatusEnum.Mixed }
        };
        
        DiaryViewModel result = DiaryService.GetDays(entries);
        
        Assert.That(result.Entries, Has.Count.EqualTo(3));
        Assert.That(result.Entries.ToList()[0].TimeStamp, Is.EqualTo(1705320002));
        Assert.That(result.Entries.ToList()[1].TimeStamp, Is.EqualTo(1705320001));
        Assert.That(result.Entries.ToList()[2].TimeStamp, Is.EqualTo(1705320000));
    }

    // Helper class for testing unknown entry types
    private class TestUnknownEntry : IDbEntry
    {
        public long Time { get; set; } = 1705320000;
    }
}
