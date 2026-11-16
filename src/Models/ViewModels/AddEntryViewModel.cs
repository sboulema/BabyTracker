using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BabyTracker.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BabyTracker.Models.ViewModels;

public class AddEntryViewModel : BaseViewModel
{
    public EntryTypeEnum Type { get; set; }

    /// <summary>Wall-clock time in the time zone of the data clone.</summary>
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime Time { get; set; }

    public string? Note { get; set; }

    public DiaperStatusEnum DiaperStatus { get; set; }

    /// <summary>Amount in ml.</summary>
    public long? FormulaAmount { get; set; }

    public int? SleepMinutes { get; set; }

    public string? ActivityDescriptionId { get; set; }

    public int? ActivityMinutes { get; set; }

    /// <summary>Weight in kg.</summary>
    public double? GrowthWeight { get; set; }

    /// <summary>Length in cm.</summary>
    public double? GrowthLength { get; set; }

    /// <summary>Head size in cm.</summary>
    public double? GrowthHeadSize { get; set; }

    public double? TemperatureCelsius { get; set; }

    public string? MilestoneSelectionId { get; set; }

    /// <summary>The photo of a Picture entry.</summary>
    public IFormFile? PictureFile { get; set; }

    public Guid? MedicationSelectionId { get; set; }

    /// <summary>Amount given, in the unit of the medication. Defaults to the amount per time of the medication.</summary>
    public double? MedicationAmount { get; set; }

    public string? VaccineSelectionId { get; set; }

    public string? SupplementSelectionId { get; set; }

    public double? SupplementAmount { get; set; }

    public string? SupplementUnit { get; set; }

    public List<SelectListItem> ActivityDescriptions { get; set; } = [];

    public List<SelectListItem> MilestoneSelections { get; set; } = [];

    public List<SelectListItem> MedicationSelections { get; set; } = [];

    public List<SelectListItem> VaccineSelections { get; set; } = [];

    public List<SelectListItem> SupplementSelections { get; set; } = [];
}
