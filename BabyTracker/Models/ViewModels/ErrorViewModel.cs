namespace BabyTracker.Models.ViewModels;

public class ErrorViewModel : BaseViewModel
{
    public string RequestId { get; set; } = string.Empty;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string Message { get; set; } = string.Empty;
}
