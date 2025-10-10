using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ModelReferences;

[ObservableModelScope(ModelScope.Singleton)]
public partial class ModelReferencesSharedModel : ObservableModel
{
    public partial string Theme { get; set; } = "Light";
    public partial string Language { get; set; } = "English";
    public partial bool NotificationsEnabled { get; set; } = true;

    [ObservableCommand(nameof(ToggleTheme))]
    public partial IObservableCommand ToggleThemeCommand { get; }

    [ObservableCommand(nameof(ToggleNotifications))]
    public partial IObservableCommand ToggleNotificationsCommand { get; }

    private void ToggleTheme()
    {
        Theme = Theme == "Light" ? "Dark" : "Light";
    }

    private void ToggleNotifications()
    {
        NotificationsEnabled = !NotificationsEnabled;
    }
}
