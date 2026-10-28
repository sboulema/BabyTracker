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
}
