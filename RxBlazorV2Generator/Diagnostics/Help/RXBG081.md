# RXBG081: ObservableModelObserver References Non-Existent Property

## Description

This error is reported when a method decorated with `[ObservableModelObserver]` references a property name that doesn't exist on the target `ObservableModel`. The generator cannot create a subscription for a non-existent property.

## Cause

This error occurs when:
- The property name in the attribute doesn't match any property on the model
- There's a typo in the property name
- The property was renamed but the observer wasn't updated
- Using `nameof()` with the wrong type's property

## How to Fix

Verify that the property name in the `[ObservableModelObserver]` attribute matches an existing property on the target model.

### Fix 1: Correct Typo in Property Name

```csharp
// WRONG - Typo in property name
[ObservableModelObserver("UserNmae")]  // Typo: "Nmae" instead of "Name"
public void OnUserNameChanged(UserModel model)
{
}

// CORRECT - Use correct property name with nameof()
[ObservableModelObserver(nameof(UserModel.UserName))]
public void OnUserNameChanged(UserModel model)
{
    Console.WriteLine($"Name changed to: {model.UserName}");
}
```

### Fix 2: Use nameof() for Type Safety

```csharp
// WRONG - Hardcoded string can be error-prone
[ObservableModelObserver("Status")]  // Might not exist or could change
public void OnStatusChanged(OrderModel model)
{
}

// CORRECT - Use nameof() for compile-time safety
[ObservableModelObserver(nameof(OrderModel.Status))]
public void OnStatusChanged(OrderModel model)
{
    LogStatusChange(model.Status);
}
```

### Fix 3: Reference Correct Model's Property

```csharp
// WRONG - Property exists on different model
[ObservableModelObserver(nameof(SettingsModel.Theme))]  // Theme is on SettingsModel
public void OnThemeChanged(UserModel model)  // But parameter is UserModel!
{
}

// CORRECT - Property and model type must match
[ObservableModelObserver(nameof(SettingsModel.Theme))]
public void OnThemeChanged(SettingsModel model)
{
    ApplyTheme(model.Theme);
}
```

### Fix 4: Update After Property Rename

```csharp
// If you renamed "Name" to "FullName" in the model:

// WRONG - Old property name
[ObservableModelObserver(nameof(UserModel.Name))]  // Property was renamed!
public void OnNameChanged(UserModel model)
{
}

// CORRECT - Use new property name
[ObservableModelObserver(nameof(UserModel.FullName))]
public void OnFullNameChanged(UserModel model)
{
    UpdateDisplayName(model.FullName);
}
```

## Best Practice: Always Use nameof()

Using `nameof()` provides compile-time safety:

```csharp
public class UserService
{
    // RECOMMENDED: nameof() catches errors at compile time
    [ObservableModelObserver(nameof(UserModel.Email))]
    public void OnEmailChanged(UserModel model)
    {
        ValidateEmail(model.Email);
    }

    // NOT RECOMMENDED: String literals can have typos
    [ObservableModelObserver("Email")]  // Works, but no compile-time check
    public void OnEmailChanged2(UserModel model)
    {
        ValidateEmail(model.Email);
    }
}
```

## Complete Example

```csharp
// Model definition
[ObservableModelScope(ModelScope.Scoped)]
public partial class CustomerModel : ObservableModel
{
    public partial string Name { get; set; }
    public partial string Email { get; set; }
    public partial Address ShippingAddress { get; set; }
    public partial bool IsActive { get; set; }

    public partial CustomerModel(CustomerService service);
}

// Service with observers
public class CustomerService
{
    // CORRECT: Property exists on CustomerModel
    [ObservableModelObserver(nameof(CustomerModel.Name))]
    public void OnNameChanged(CustomerModel model)
    {
        LogChange("Name", model.Name);
    }

    // CORRECT: Property exists on CustomerModel
    [ObservableModelObserver(nameof(CustomerModel.Email))]
    public async Task OnEmailChangedAsync(CustomerModel model)
    {
        await SendWelcomeEmailAsync(model.Email);
    }

    // WRONG: "Address" doesn't exist - it's "ShippingAddress"!
    // This will cause RXBG081
    [ObservableModelObserver("Address")]
    public void OnAddressChanged(CustomerModel model)
    {
        // This observer won't be wired up
    }

    // CORRECT: Use the actual property name
    [ObservableModelObserver(nameof(CustomerModel.ShippingAddress))]
    public void OnShippingAddressChanged(CustomerModel model)
    {
        UpdateShippingOptions(model.ShippingAddress);
    }
}
```

## Verifying Property Names

To find the correct property name:

1. **Check the Model Class**: Open the model class and verify the exact property names
2. **Use IntelliSense**: Type `nameof(ModelType.` and IntelliSense will show available properties
3. **Search the Codebase**: Search for the property declaration in your project

```csharp
// The model defines these properties:
public partial class MyModel : ObservableModel
{
    public partial string FirstName { get; set; }      // Use: nameof(MyModel.FirstName)
    public partial string LastName { get; set; }       // Use: nameof(MyModel.LastName)
    public partial DateTime BirthDate { get; set; }    // Use: nameof(MyModel.BirthDate)
    public partial bool IsVerified { get; set; }       // Use: nameof(MyModel.IsVerified)
}
```

## Common Mistakes

| Mistake | Problem | Solution |
|---------|---------|----------|
| `"name"` | Case-sensitive mismatch | Use `nameof(Model.Name)` |
| `"UserName"` when property is `"User"` | Wrong property name | Verify exact property name |
| `nameof(OtherModel.Prop)` | Wrong model type | Match model type to parameter |
| `"Status"` after rename to `"State"` | Outdated reference | Update to new property name |

## Severity

**Error** - This diagnostic will block code generation for the invalid observer method. The subscription will not be created.

## Related Diagnostics

- RXBG080: ObservableModelObserver method has invalid signature
- RXBG012: Referenced model has no used properties
