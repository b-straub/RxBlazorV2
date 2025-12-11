# RXBG080: ObservableModelObserver Method Has Invalid Signature

## Description

This error is reported when a method decorated with `[ObservableModelObserver]` has an invalid signature. The source generator cannot wire up subscriptions for methods that don't follow the required patterns.

## Cause

This error occurs when:
- The method has no parameters
- The first parameter is not the target `ObservableModel` type
- A sync method (returning `void`) has more than one parameter
- A sync method returns a value other than `void`
- An async method has more than two parameters
- An async method's second parameter is not `CancellationToken`

## Valid Method Signatures

### Sync Methods

Sync observer methods must:
- Return `void`
- Have exactly one parameter: the model type

```csharp
// Valid sync signature
[ObservableModelObserver(nameof(MyModel.UserName))]
public void OnUserNameChanged(MyModel model)
{
    // Handle property change
}
```

### Async Methods

Async observer methods must:
- Return `Task` (or `ValueTask`)
- Have one or two parameters: the model type, optionally followed by `CancellationToken`

```csharp
// Valid async signature (without CancellationToken)
[ObservableModelObserver(nameof(MyModel.Settings))]
public async Task OnSettingsChangedAsync(MyModel model)
{
    await SaveSettingsAsync(model.Settings);
}

// Valid async signature (with CancellationToken)
[ObservableModelObserver(nameof(MyModel.Settings))]
public async Task OnSettingsChangedAsync(MyModel model, CancellationToken ct)
{
    await SaveSettingsAsync(model.Settings, ct);
}
```

## How to Fix

Change the method signature to match one of the valid patterns.

### Fix 1: Sync Method - Add Model Parameter

```csharp
// WRONG - No parameters
[ObservableModelObserver(nameof(MyModel.Count))]
public void OnCountChanged()  // Missing model parameter
{
}

// CORRECT - Add model parameter
[ObservableModelObserver(nameof(MyModel.Count))]
public void OnCountChanged(MyModel model)
{
    Console.WriteLine($"Count is now: {model.Count}");
}
```

### Fix 2: Sync Method - Remove Extra Parameters

```csharp
// WRONG - Too many parameters
[ObservableModelObserver(nameof(MyModel.Name))]
public void OnNameChanged(MyModel model, string oldValue)  // Extra parameter not allowed
{
}

// CORRECT - Only model parameter
[ObservableModelObserver(nameof(MyModel.Name))]
public void OnNameChanged(MyModel model)
{
    // Access model.Name directly
}
```

### Fix 3: Sync Method - Change Return Type to Void

```csharp
// WRONG - Returns value
[ObservableModelObserver(nameof(MyModel.Status))]
public bool OnStatusChanged(MyModel model)  // Must return void
{
    return model.Status == "Active";
}

// CORRECT - Returns void
[ObservableModelObserver(nameof(MyModel.Status))]
public void OnStatusChanged(MyModel model)
{
    if (model.Status == "Active")
    {
        // Handle active status
    }
}
```

### Fix 4: Async Method - Fix CancellationToken Parameter

```csharp
// WRONG - Second parameter is not CancellationToken
[ObservableModelObserver(nameof(MyModel.Data))]
public async Task OnDataChangedAsync(MyModel model, int delay)  // Wrong type
{
    await Task.Delay(delay);
}

// CORRECT - Use CancellationToken as second parameter
[ObservableModelObserver(nameof(MyModel.Data))]
public async Task OnDataChangedAsync(MyModel model, CancellationToken ct)
{
    await ProcessDataAsync(model.Data, ct);
}
```

### Fix 5: Wrong Model Type

```csharp
// WRONG - Parameter type doesn't match the model the service observes
[ObservableModelObserver(nameof(UserModel.Email))]
public void OnEmailChanged(SettingsModel model)  // Wrong model type!
{
}

// CORRECT - Use the correct model type
[ObservableModelObserver(nameof(UserModel.Email))]
public void OnEmailChanged(UserModel model)
{
    ValidateEmail(model.Email);
}
```

## Complete Example

```csharp
// Service class with observer methods
public class NotificationService
{
    // Sync observer - notifies on status change
    [ObservableModelObserver(nameof(OrderModel.Status))]
    public void OnOrderStatusChanged(OrderModel model)
    {
        SendNotification($"Order {model.Id} status: {model.Status}");
    }

    // Async observer without cancellation - saves to database
    [ObservableModelObserver(nameof(OrderModel.Items))]
    public async Task OnItemsChangedAsync(OrderModel model)
    {
        await _database.SaveOrderItemsAsync(model.Id, model.Items);
    }

    // Async observer with cancellation - external API call
    [ObservableModelObserver(nameof(OrderModel.Total))]
    public async Task OnTotalChangedAsync(OrderModel model, CancellationToken ct)
    {
        await _paymentService.UpdateQuoteAsync(model.Id, model.Total, ct);
    }
}

// Model that uses the service
[ObservableModelScope(ModelScope.Scoped)]
public partial class OrderModel : ObservableModel
{
    public partial string Status { get; set; }
    public partial List<OrderItem> Items { get; set; }
    public partial decimal Total { get; set; }
    public int Id { get; set; }

    // Service injected - observers auto-subscribed
    public partial OrderModel(NotificationService notificationService);
}
```

## Generated Code

For valid observer methods, the generator creates subscriptions in `OnContextReadyIntern()`:

```csharp
protected override void OnContextReadyIntern()
{
    // Sync observer subscription
    Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Status"]).Any())
        .Subscribe(_ => NotificationService.OnOrderStatusChanged(this)));

    // Async observer without CT
    Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Items"]).Any())
        .SubscribeAwait(async (_, _) =>
        {
            await NotificationService.OnItemsChangedAsync(this);
        }, AwaitOperation.Switch));

    // Async observer with CT
    Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Total"]).Any())
        .SubscribeAwait(async (_, ct) =>
        {
            await NotificationService.OnTotalChangedAsync(this, ct);
        }, AwaitOperation.Switch));
}
```

## Summary Table

| Pattern | Return Type | Parameters | Valid |
|---------|-------------|------------|-------|
| Sync | `void` | `(ModelType model)` | Yes |
| Sync | `void` | `()` | No |
| Sync | `void` | `(ModelType model, ...)` | No |
| Sync | `bool` | `(ModelType model)` | No |
| Async | `Task` | `(ModelType model)` | Yes |
| Async | `Task` | `(ModelType model, CancellationToken ct)` | Yes |
| Async | `Task` | `()` | No |
| Async | `Task` | `(ModelType model, int extra)` | No |
| Async | `Task` | `(ModelType model, CancellationToken ct, ...)` | No |

## Severity

**Error** - This diagnostic will block code generation for the invalid observer method. The subscription will not be created.

## Related Diagnostics

- RXBG081: ObservableModelObserver references non-existent property
- RXBG050: Unregistered service warning
