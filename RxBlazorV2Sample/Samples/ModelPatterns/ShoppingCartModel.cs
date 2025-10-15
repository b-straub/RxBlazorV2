using R3;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Samples.ModelPatterns;

/// <summary>
/// Shopping cart model - demonstrates partial constructor pattern.
/// Injects ProductCatalogModel via constructor to automatically recalculate total when prices change.
///
/// Use partial constructor injection when:
/// - Your model needs to REACT to changes in another model (side effects/business logic)
/// - You need to perform calculations or updates based on referenced model changes
/// - The relationship is at the model/business logic level
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ShoppingCartModel : ObservableModel
{
    public partial int LaptopQuantity { get; set; } = 0;
    public partial int MouseQuantity { get; set; } = 0;
    public partial int KeyboardQuantity { get; set; } = 0;

    // Total is calculated based on quantities and prices from ProductCatalogModel
    public partial decimal Total { get; set; } = 0m;

    // Declare partial constructor with ProductCatalogModel dependency
    public partial ShoppingCartModel(ProductCatalogModel productCatalog);

    private CompositeDisposable? _localSubscriptions;

    protected override void OnContextReady()
    {
        // Subscribe to any property changes (quantities or prices) to recalculate total
        // ProductCatalog.Observable is automatically merged via constructor injection
        _localSubscriptions = new CompositeDisposable();
        _localSubscriptions.Add(Observable.Subscribe(props => RecalculateTotal()));

        // Initial calculation
        RecalculateTotal();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _localSubscriptions?.Dispose();
        }
        base.Dispose(disposing);
    }

    // This method is called when quantities change OR when ProductCatalog prices change (via merged Observable)
    private void RecalculateTotal()
    {
        Total = (LaptopQuantity * ProductCatalog.LaptopPrice) +
                (MouseQuantity * ProductCatalog.MousePrice) +
                (KeyboardQuantity * ProductCatalog.KeyboardPrice);
    }

    [ObservableCommand(nameof(AddLaptop))]
    public partial IObservableCommand AddLaptopCommand { get; }

    [ObservableCommand(nameof(AddMouse))]
    public partial IObservableCommand AddMouseCommand { get; }

    [ObservableCommand(nameof(AddKeyboard))]
    public partial IObservableCommand AddKeyboardCommand { get; }

    [ObservableCommand(nameof(ClearCart))]
    public partial IObservableCommand ClearCartCommand { get; }

    private void AddLaptop()
    {
        LaptopQuantity++;
    }

    private void AddMouse()
    {
        MouseQuantity++;
    }

    private void AddKeyboard()
    {
        KeyboardQuantity++;
    }

    private void ClearCart()
    {
        LaptopQuantity = 0;
        MouseQuantity = 0;
        KeyboardQuantity = 0;
    }
}
