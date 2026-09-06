# RXBG044: Observable Component Batch Has No Effect

## Description

This diagnostic is reported when a model declares an `[ObservableComponentBatchAsync]` batch but has no `[ObservableComponent]` attribute. The batch generates its hook on the model's generated component, so without a component there is nothing to generate and the attribute has no effect.

## Cause

```csharp
// ❌ RXBG044: no [ObservableComponent], so no OnSearchBatchChangedAsync is generated
public partial class SearchModel : ObservableModel
{
    [ObservableComponentBatchAsync("search", 250)]
    public partial string SearchTerm { get; set; } = "";

    [ObservableComponentBatchAsync("search")]
    public partial bool HighlightMatches { get; set; }
}
```

**Message:**

```
RXBG044: Batch 'search' in model 'SearchModel' is declared with [ObservableComponentBatchAsync],
but this model has no [ObservableComponent] attribute. The batch generates its hook on the model's
component, so without it no hook is generated and the attribute has no effect. Either add
[ObservableComponent] to this model, or remove the attribute.
```

### Why This Is a Problem

Everything the attribute produces lives on the generated component:

```csharp
// <Model>Component.g.cs
Subscriptions.Add(R3.Observable.Merge(
        Model.Observable.Where(p => p.Intersect(["Model.HighlightMatches"]).Any()),
        Model.Observable.Where(p => p.Intersect(["Model.SearchTerm"]).Any())
            .Debounce(TimeSpan.FromMilliseconds(250)))
    .SubscribeAwait(async (props, ct) =>
    {
        await OnSearchBatchChangedAsync(ct);
    }, AwaitOperation.Switch));

protected virtual Task OnSearchBatchChangedAsync(CancellationToken ct) => Task.CompletedTask;
```

No component means no subscription and no hook. This is usually discovered as a compile error on an `override` of a method that does not exist.

Unlike [RXBG041](RXBG041.md), this cannot be satisfied by having another model reference this one — batches are resolved from the model's own properties only, so referenced-model propagation does not apply.

## How to Fix

### Option 1: Add [ObservableComponent] (Recommended)

A batch exists to drive a component-level side effect, so the model needs a component:

```csharp
// ✅ Generates SearchModelComponent with OnSearchBatchChangedAsync
[ObservableComponent]
public partial class SearchModel : ObservableModel
{
    [ObservableComponentBatchAsync("search", 250)]
    public partial string SearchTerm { get; set; } = "";

    [ObservableComponentBatchAsync("search")]
    public partial bool HighlightMatches { get; set; }
}
```

```razor
@* SearchPage.razor *@
@inherits SearchModelComponent

@code {
    private MudTable<SearchHit>? _table;

    protected override async Task OnSearchBatchChangedAsync(CancellationToken ct)
    {
        if (_table is not { } table)
        {
            return;
        }

        try
        {
            await table.ReloadServerData();
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer query.
        }
    }
}
```

### Option 2: Remove the Attribute

If the properties do not drive a component side effect, drop it. If what you actually wanted was to group them for a `SuspendNotifications` scope, that is the unrelated `[ObservableBatch]`:

```csharp
// ✅ Plain notification grouping - no hook, no component required, no warning
public partial class SearchModel : ObservableModel
{
    [ObservableBatch("search")]
    public partial string SearchTerm { get; set; } = "";

    [ObservableBatch("search")]
    public partial bool HighlightMatches { get; set; }

    private void ResetSearch()
    {
        using (SuspendNotifications("search"))
        {
            SearchTerm = "";
            HighlightMatches = false;
            // One notification fires when the scope is disposed.
        }
    }
}
```

Both fixes are offered as IDE code fixes on this diagnostic.

## Severity

**Warning** — the code compiles, but the attribute has no effect and the hook you intended to override does not exist.

## Related Diagnostics

- [RXBG041](RXBG041.md): ObservableComponentTrigger attribute has no effect (the same shape of mistake for per-property component triggers)
- [RXBG043](RXBG043.md): Invalid observable component batch declaration

## See Also

- `[ObservableComponentBatchAsync]` attribute documentation
- `[ObservableComponent]` attribute documentation
- Component Batch Triggers in the reactive patterns guide
