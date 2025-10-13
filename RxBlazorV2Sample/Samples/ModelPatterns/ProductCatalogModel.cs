using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ModelPatterns;

/// <summary>
/// Product catalog model - manages product prices and availability.
/// This is a standalone model that other models can reference.
/// </summary>
[ObservableModelScope(ModelScope.Singleton)]
public partial class ProductCatalogModel : ObservableModel
{
    public partial decimal LaptopPrice { get; set; } = 999.99m;
    public partial decimal MousePrice { get; set; } = 29.99m;
    public partial decimal KeyboardPrice { get; set; } = 79.99m;

    [ObservableCommand(nameof(ApplyDiscount))]
    public partial IObservableCommand ApplyDiscountCommand { get; }

    private void ApplyDiscount()
    {
        LaptopPrice *= 0.9m; // 10% discount
        MousePrice *= 0.9m;
        KeyboardPrice *= 0.9m;
    }
}
