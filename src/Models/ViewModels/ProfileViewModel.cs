using BabyTracker.Constants;

namespace BabyTracker.Models.ViewModels;

public class ProfileViewModel : BaseViewModel
{
	public string EmailAddress { get; set; } = string.Empty;

	public string NewEmailAddress { get; set; } = string.Empty;
	public bool EnableMemoriesEmail { get; set; }

	public string MemoriesAddresses { get; set; } = string.Empty;

	public string ShareList { get; set; } = string.Empty;

	public int FontSize { get; set; } = 6;

	public ThemesEnum Theme { get; set; }
	
	public bool UseFullCardImages { get; set; }
	
	public bool UseCards { get; set; }

	/// <summary>Whether syncing to the Baby Tracker app is switched on for this website at all.</summary>
	public bool SyncAvailable { get; set; }

	public bool SyncEnabled { get; set; }

	public string SyncEmailAddress { get; set; } = string.Empty;

	public bool SyncHasPassword { get; set; }

	public string SyncDeviceId { get; set; } = string.Empty;
}
