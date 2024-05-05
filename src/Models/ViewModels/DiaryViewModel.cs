using System;
using System.Collections.Generic;
using System.Linq;
using BabyTracker.Models.Database;

namespace BabyTracker.Models.ViewModels;

public class DiaryViewModel : BaseViewModel
{
	public IEnumerable<DiaryEntryViewModel> Entries { get; set; } = [];

	public List<string> EntryTypes { get; set; } =
    [
        "Diaper", 
		"Formula", 
		"Supplement", 
		"Joy", 
		"Growth", 
		"Medication", 
		"Milestone", 
		"Activity", 
		"Sleep", 
		"Temperature",
		"Vaccine"
	];

	public DateOnly Date { get; set; }

	public List<DateOnly> AvailableDates = [];

	public string DateNextUrl { get; set; } = string.Empty;

	public string DatePreviousUrl { get; set; } = string.Empty;

	public int FontSize { get; set; } = 6;
	
	public bool UseCards { get; set; }
}
