using ObservableCollections;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Status.Models;

/// <summary>
/// Application status domain model - consolidated status and error management.
///
/// Patterns demonstrated:
/// - Singleton scope for global status aggregation
/// - [ObservableComponent] for UI generation
/// - [ObservableComponentTrigger] for real-time status bar updates
/// - ObservableList for reactive message collection
///
/// File organization:
/// - AppStatusModel.cs: Properties
/// - AppStatusModel.Commands.cs: Commands with their implementations
/// - AppStatusModel.Methods.cs: Public API (AddInfo, AddSuccess, etc.)
///
/// Note: Named AppStatusModel to avoid conflict with RxBlazorV2.MudBlazor.Components.StatusModel
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class AppStatusModel : ObservableModel
{
    /// <summary>
    /// Reactive collection of status messages.
    /// </summary>
    [ObservableComponentTrigger]
    public partial ObservableList<StatusMessage> Messages { get; init; } = [];

    /// <summary>
    /// Indicates whether there are any error messages.
    /// </summary>
    [ObservableComponentTrigger]
    public partial bool HasErrors { get; set; }

    /// <summary>
    /// Indicates whether there are any warning messages.
    /// </summary>
    [ObservableComponentTrigger]
    public partial bool HasWarnings { get; set; }

    /// <summary>
    /// The most recent error message, if any.
    /// </summary>
    [ObservableComponentTrigger]
    public partial string? LastError { get; set; }

    /// <summary>
    /// The most recent status message (any severity).
    /// </summary>
    [ObservableComponentTrigger]
    public partial string? LastMessage { get; set; }

    /// <summary>
    /// Total count of messages.
    /// </summary>
    public partial int MessageCount { get; set; }

    /// <summary>
    /// Whether the status panel is expanded.
    /// </summary>
    public partial bool IsPanelExpanded { get; set; }
}
