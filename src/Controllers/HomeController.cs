using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using BabyTracker.Constants;
using BabyTracker.Extensions;
using BabyTracker.Filters;
using BabyTracker.Models.Database;
using BabyTracker.Models.ViewModels;
using BabyTracker.Services;
using BabyTracker.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.OutputCaching;

namespace BabyTracker.Controllers;

[Route("")]
public class HomeController(
	IImportService importService,
	ISqLiteService sqLiteService,
	IChartService chartService,
	IAccountService accountService,
	IAddEntryService addEntryService,
	ICacheEvictionService cacheEvictionService,
	IImageService imageService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // User not logged in
        if (User.Identity?.IsAuthenticated != true)
        {
            ViewBag.Theme = ThemesEnum.Auto;

            return View(new BaseViewModel());
        }

        // User logged in but no data clone available
        if (!importService.HasDataClone(User))
        {
            return Redirect("/import");
        }

        // User logged in and has a data clone available
        await sqLiteService.OpenDataConnectionAsync(User);

        var babiesViewModel = new BabiesViewModel
        {
            Babies = await sqLiteService.GetBabiesFromDb(),
            NickName = User.FindFirstValue("nickname") ?? string.Empty,
            ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
            UserId = User.FindFirstValue("activeUserId") ?? string.Empty,
        };

        await sqLiteService.CloseDataConnection();

        var userMetaData = await accountService.GetUserMetaData(User);
        ViewBag.Theme = userMetaData?.Theme;

        return View("Babies", babiesViewModel);
    }

    [OutputCache(PolicyName = "AuthenticatedOutputCache")]
	[Authorize]
    [ServiceFilter<RequireDataCloneFilter>]
    [HttpGet("{babyName}/{date?}")]
	public async Task<IActionResult> Diary(string babyName, DateOnly? date, string? q)
	{
		await sqLiteService.OpenDataConnectionAsync(User);

		var availableDates = await sqLiteService.GetAllEntryDates(babyName);

		if (date == null)
		{
			date = availableDates.LastOrDefault();

			if (date == null)
			{
				date = DateOnly.FromDateTime(DateTime.Now);
			}
		}

		var entries = new List<IDbEntry>();

		// Do we need to show entries from a specific day
		// or do we have a search query?
		if (!string.IsNullOrEmpty(q))
		{
			entries = await sqLiteService.Search(q, babyName);
		}
		else
		{
			entries = await sqLiteService.GetEntriesFromDb(date.Value, babyName);
		}

		var model = DiaryService.GetDays(entries);

		var dateNext = availableDates.SkipWhile(availableDate => availableDate != date).Skip(1).FirstOrDefault();
		var datePrevious = availableDates.TakeWhile(availableDate => availableDate != date).LastOrDefault();

		model.AvailableDates = availableDates;
		model.Date = date.Value;
		model.DateNextUrl = dateNext != DateOnly.MinValue ? $"/{babyName}/{dateNext:yyyy-MM-dd}" : string.Empty;
		model.DatePreviousUrl = datePrevious != DateOnly.MinValue ? $"/{babyName}/{datePrevious:yyyy-MM-dd}" : string.Empty;
		model.BabyName = babyName;

		model.NickName = User.FindFirstValue("nickname") ?? string.Empty;
		model.ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty;
		model.UserId = User.FindFirstValue("activeUserId") ?? string.Empty;

		var memories = await sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);
		model.MemoriesBadgeCount = memories.Count;
		model.ShowMemoriesLink = true;

		var lastEntryDateTime = await sqLiteService.GetLastEntryDateTime(babyName);
		ViewData["LastEntry"] = lastEntryDateTime;

		var userMetaData = await accountService.GetUserMetaData(User);
		model.FontSize = userMetaData?.FontSize ?? 6;
		model.UseCards = userMetaData?.UseCards ?? false;
		ViewBag.Theme = userMetaData?.Theme;
		ViewBag.UseFullCardImages = userMetaData?.UseFullCardImages;

		if ((date == null || date.Value == DateOnly.FromDateTime(lastEntryDateTime)) &&
			userMetaData?.LastViewedDate != null &&
			userMetaData?.LastViewedDate != date)
		{
			TempData["notificationMessage"] = "Do you want to continue where you left off? " + 
				$"<a href=\"{babyName}/{userMetaData!.LastViewedDate!.Value:yyyy-MM-dd}\" class=\"alert-link\">{userMetaData!.LastViewedDate!.Value:yyyy-MM-dd}</a>";
			TempData["notificationType"] = "info";
		}

		if (date != null && date.Value != DateOnly.FromDateTime(lastEntryDateTime))
		{
			userMetaData!.LastViewedDate = date;
			await accountService.SaveUserMetaData(User, userMetaData);
		}

		await sqLiteService.CloseDataConnection();

		return View("Diary", model);
	}

	[Authorize]
    [ServiceFilter<RequireDataCloneFilter>]
    [HttpGet("{babyName}/memories")]
	public async Task<IActionResult> Memories(string babyName)
	{
		await sqLiteService.OpenDataConnectionAsync(User);

		var memories = await sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

		await sqLiteService.CloseDataConnection();

		var model = DiaryService.GetDays(memories);

		model.BabyName = babyName;
		model.MemoriesBadgeCount = memories.Count;
		model.ShowMemoriesLink = true;

		model.NickName = User.FindFirstValue("nickname") ?? string.Empty;
		model.ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty;
		model.UserId = User.FindFirstValue("activeUserId") ?? string.Empty;

		var userMetaData = await accountService.GetUserMetaData(User);
		ViewBag.Theme = userMetaData?.Theme;
		ViewBag.UseFullCardImages = userMetaData?.UseFullCardImages;

		return View("Memories", model);
	}

	[Authorize]
    [ServiceFilter<RequireDataCloneFilter>]
    [HttpGet("{babyName}/charts/{months?}")]
	public async Task<IActionResult> Charts(string babyName, int? months = null)
	{
		await sqLiteService.OpenDataConnectionAsync(User);

		var memories = await sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

		await sqLiteService.CloseDataConnection();

		var model = await chartService.GetViewModel(User, babyName, months + 1);

		model.BabyName = babyName;
		model.MemoriesBadgeCount = memories.Count;
		model.ShowMemoriesLink = true;

		model.NickName = User.FindFirstValue("nickname") ?? string.Empty;
		model.ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty;
		model.UserId = User.FindFirstValue("activeUserId") ?? string.Empty;

		var userMetaData = await accountService.GetUserMetaData(User);
		ViewBag.Theme = userMetaData?.Theme;

		return View("Charts", model);
	}

	[Authorize]
    [ServiceFilter<RequireDataCloneFilter>]
    [HttpGet("{babyName}/gallery")]
	public async Task<IActionResult> Gallery(string babyName)
	{
		await sqLiteService.OpenDataConnectionAsync(User);

		var pictures = await sqLiteService.GetPictures(babyName);
		var memories = await sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

		await sqLiteService.CloseDataConnection();

		var model = new GalleryViewModel
		{
			Pictures = pictures,
			BabyName = babyName,
			MemoriesBadgeCount = memories.Count,
			ShowMemoriesLink = true,
			NickName = User.FindFirstValue("nickname") ?? string.Empty,
			ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
			UserId = User.FindFirstValue("activeUserId") ?? string.Empty
		};

		var userMetaData = await accountService.GetUserMetaData(User);
		ViewBag.Theme = userMetaData?.Theme;

		return View("Gallery", model);
	}

	[Authorize]
    [ServiceFilter<RequireDataCloneFilter>]
    [HttpGet("{babyName}/add/{date?}")]
	public async Task<IActionResult> AddEntry(string babyName, DateOnly? date)
	{
		// Default to the current time of day, on the day that is being viewed
		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToDateTimeLocal();
		var day = date ?? DateOnly.FromDateTime(now);

		var model = await PrepareAddEntryViewModel(new AddEntryViewModel
		{
			Time = day.ToDateTime(new TimeOnly(now.Hour, now.Minute))
		}, babyName);

		if (model == null)
		{
			return NotFound();
		}

		return View("AddEntry", model);
	}

	[Authorize]
    [ServiceFilter<RequireDataCloneFilter>]
    [HttpPost("{babyName}/add/{date?}")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> AddEntry(string babyName, AddEntryViewModel model)
	{
		var activeUserId = User.FindFirstValue("activeUserId");

		if (string.IsNullOrEmpty(activeUserId))
		{
			return Forbid();
		}

		ValidateEntry(model);

		ProcessedPicture? picture = null;

		if (model.Type == EntryTypeEnum.Picture && ModelState.IsValid)
		{
			picture = await imageService.CreateJpegAndThumbnailAsync(model.PictureFile!);

			if (picture == null)
			{
				ModelState.AddModelError(nameof(model.PictureFile), "This is not a supported image. Use a JPEG, PNG or WebP photo.");
			}
		}

		if (!ModelState.IsValid)
		{
			var invalidModel = await PrepareAddEntryViewModel(model, babyName);

			if (invalidModel == null)
			{
				return NotFound();
			}

			return View("AddEntry", invalidModel);
		}

		AddEntryResult result;

		try
		{
			result = await addEntryService.AddEntryAsync(activeUserId, babyName, model, picture);
		}
		catch (KeyNotFoundException)
		{
			return NotFound();
		}

		// The Diary is output-cached: drop the pages of everyone viewing this clone
		await cacheEvictionService.EvictClonePagesAsync(activeUserId);
		await cacheEvictionService.EvictUserPagesAsync(User);

		switch (result.Sync.Status)
		{
			case SyncStatus.Sent:
				TempData["notificationMessage"] = $"{model.Type} entry added and sent to the Baby Tracker app.";
				TempData["notificationType"] = "success";
				break;

			case SyncStatus.Failed:
				TempData["notificationMessage"] =
					$"{model.Type} entry added, but it could not be sent to the Baby Tracker app: {WebUtility.HtmlEncode(result.Sync.Message ?? "unknown error")}";
				TempData["notificationType"] = "warning";
				break;

			default:
				TempData["notificationMessage"] = $"{model.Type} entry added.";
				TempData["notificationType"] = "success";
				break;
		}

		return RedirectToAction(nameof(Diary), new
		{
			babyName,
			date = DateOnly.FromDateTime(model.Time).ToString("yyyy-MM-dd")
		});
	}

	private void ValidateEntry(AddEntryViewModel model)
	{
		switch (model.Type)
		{
			case EntryTypeEnum.Formula when model.FormulaAmount is not > 0:
				ModelState.AddModelError(nameof(model.FormulaAmount), "Enter the amount in ml.");
				break;

			case EntryTypeEnum.Sleep when model.SleepMinutes is not > 0:
				ModelState.AddModelError(nameof(model.SleepMinutes), "Enter the duration in minutes.");
				break;

			case EntryTypeEnum.Activity when model.ActivityDescriptionId is null:
				ModelState.AddModelError(nameof(model.ActivityDescriptionId), "Choose an activity.");
				break;

			case EntryTypeEnum.Growth when model.GrowthWeight is null &&
				model.GrowthLength is null && model.GrowthHeadSize is null:
				ModelState.AddModelError(nameof(model.GrowthWeight), "Enter at least one measurement.");
				break;

			case EntryTypeEnum.Temperature when model.TemperatureCelsius is not (>= 30 and <= 45):
				ModelState.AddModelError(nameof(model.TemperatureCelsius), "Enter a temperature between 30 and 45 °C.");
				break;

			case EntryTypeEnum.Milestone when model.MilestoneSelectionId is null:
				ModelState.AddModelError(nameof(model.MilestoneSelectionId), "Choose a milestone.");
				break;

			case EntryTypeEnum.Picture when model.PictureFile is not { Length: > 0 }:
				ModelState.AddModelError(nameof(model.PictureFile), "Choose a photo.");
				break;

			case EntryTypeEnum.Medication when model.MedicationSelectionId is null:
				ModelState.AddModelError(nameof(model.MedicationSelectionId), "Choose a medication.");
				break;

			case EntryTypeEnum.Vaccine when string.IsNullOrEmpty(model.VaccineSelectionId):
				ModelState.AddModelError(nameof(model.VaccineSelectionId), "Choose a vaccine.");
				break;

			case EntryTypeEnum.Supplement when model.SupplementSelectionId is null:
				ModelState.AddModelError(nameof(model.SupplementSelectionId), "Choose a supplement.");
				break;

			case EntryTypeEnum.Supplement when model.SupplementAmount is not > 0:
				ModelState.AddModelError(nameof(model.SupplementAmount), "Enter the amount.");
				break;
		}
	}

	/// <returns>The model with the layout data and dropdown options filled in, or null when the baby does not exist.</returns>
	private async Task<AddEntryViewModel?> PrepareAddEntryViewModel(AddEntryViewModel model, string babyName)
	{
		await sqLiteService.OpenDataConnectionAsync(User);

		var babies = await sqLiteService.GetBabiesFromDb();

        if (!babies.Any(baby => string.Equals(baby.Name, babyName, StringComparison.OrdinalIgnoreCase)))
        {
            await sqLiteService.CloseDataConnection();
            return null;
        }

        var memories = await sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);
		var activityDescriptions = await sqLiteService.GetActivityDescriptionsFromDb();
		var milestoneSelections = await sqLiteService.GetMilestoneSelectionsFromDb();
		var medicationSelections = await sqLiteService.GetMedicationSelectionsFromDb();
		var vaccineSelections = await sqLiteService.GetVaccineSelectionsFromDb();
		var supplementSelections = await sqLiteService.GetSupplementSelectionsFromDb();

		await sqLiteService.CloseDataConnection();

		model.ActivityDescriptions = activityDescriptions
			.Select(description => new SelectListItem(description.Name, description.Id.ToString()))
			.ToList();
		model.MilestoneSelections = milestoneSelections
			.Select(selection => new SelectListItem(selection.Name, selection.Id.ToString()))
			.ToList();
		model.MedicationSelections = medicationSelections
			.Select(selection => new SelectListItem(
				$"{selection.Name} ({selection.AmountPerTime} {selection.Unit})", selection.Id.ToString()))
			.ToList();
		model.VaccineSelections = vaccineSelections
			.Select(selection => new SelectListItem(
				string.IsNullOrEmpty(selection.Description) ? selection.Name : $"{selection.Name} - {selection.Description}",
				selection.Id))
			.ToList();
		model.SupplementSelections = supplementSelections
			.Select(selection => new SelectListItem(selection.Name, selection.Id.ToString()))
			.ToList();

		model.BabyName = babyName;
		model.MemoriesBadgeCount = memories.Count;
		model.ShowMemoriesLink = true;

		model.NickName = User.FindFirstValue("nickname") ?? string.Empty;
		model.ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty;
		model.UserId = User.FindFirstValue("activeUserId") ?? string.Empty;

		var userMetaData = await accountService.GetUserMetaData(User);
		ViewBag.Theme = userMetaData?.Theme;

		return model;
	}
}
