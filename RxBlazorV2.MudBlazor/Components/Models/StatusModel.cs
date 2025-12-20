using ObservableCollections;
using RxBlazorV2.Model;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.MudBlazor.Components;

/// <summary>
/// Concrete singleton StatusModel for MudBlazor applications.
/// Inherits Messages, status methods, and component triggers from the abstract base.
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
// ReSharper disable once ClassNeverInstantiated.Global
public partial class StatusModel : StatusBaseModel
{
    // Inherits everything from base class:
    // - Messages: ObservableList<StatusMessage> with [ObservableComponentTrigger]
    // - MessageMode: StatusMessageMode (Aggregate or Single)
    // - HandleError(Exception, commandName, methodName)
    // - AddInfo, AddSuccess, AddWarning, AddError methods
    // - HasErrors, HasWarnings, ErrorCount computed properties
    // - ClearMessages()
    public override ObservableList<StatusMessage> Messages { get; } = [];
}