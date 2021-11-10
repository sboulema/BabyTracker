namespace BabyTracker.Models.ViewModels;

public class LoginViewModel : BaseViewModel
{
    public string EmailAddress { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
