using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ModelPatterns;

/// <summary>
/// User profile model - manages user information.
/// This model is injected into components that need to display user info.
/// </summary>
[ObservableModelScope(ModelScope.Scoped)]
public partial class UserProfileModel : ObservableModel
{
    public partial string UserName { get; set; } = "John Doe";
    public partial string Email { get; set; } = "john.doe@example.com";
    public partial string ShippingAddress { get; set; } = "123 Main St, Anytown, USA";
    public partial bool IsPremiumMember { get; set; } = false;

    [ObservableCommand(nameof(TogglePremium))]
    public partial IObservableCommand TogglePremiumCommand { get; }

    private void TogglePremium()
    {
        IsPremiumMember = !IsPremiumMember;
    }
}
