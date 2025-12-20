using ObservableCollections;
using RxBlazorV2.Model;

namespace RxBlazorVSSampleComponents.ErrorManager;

/// <summary>
/// Concrete singleton StatusModel for error and status handling in the sample.
/// Inherits Messages, status methods, and component triggers from the abstract base.
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class ErrorModel : StatusBaseModel
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
