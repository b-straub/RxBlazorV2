using ReactivePatternSample.Settings.Models;
using ReactivePatternSample.Status.Models;
using ReactivePatternSample.Todo.Models;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Share.Models;

/// <summary>
/// Share domain model - handles sharing and exporting of todo items.
///
/// Patterns demonstrated:
/// - Singleton scope (shared state between ShareButton and ShareDialog)
/// - [ObservableComponent] for UI generation
/// - Partial constructor for cross-domain DI (Todo, Status, Settings)
/// - Auto-export via internal observer when Settings.PreferredExportFormat changes
/// - Auto-export via trigger when dialog opens
///
/// File organization:
/// - ShareModel.cs: Constructor and properties
/// - ShareModel.Commands.cs: Commands with their implementations
/// - ShareModel.Observers.cs: Internal observer methods
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class ShareModel : ObservableModel
{
    /// <summary>
    /// Partial constructor - generator creates DI injection for referenced models.
    /// Settings reference allows auto-export when format changes.
    /// </summary>
    public partial ShareModel(TodoListModel todo, StatusModel status, SettingsModel settings);

    /// <summary>
    /// Whether the share dialog is open.
    /// When opened, triggers auto-export with current format.
    /// </summary>
    [ObservableTrigger(nameof(AutoExportOnDialogOpen))]
    public partial bool IsDialogOpen { get; set; }

    /// <summary>
    /// The exported text content.
    /// </summary>
    public partial string ExportedText { get; set; } = string.Empty;

    /// <summary>
    /// Whether an export operation is in progress.
    /// </summary>
    public partial bool IsExporting { get; set; }

    /// <summary>
    /// Whether a copy operation was successful (for feedback).
    /// </summary>
    public partial bool CopySuccess { get; set; }
}
