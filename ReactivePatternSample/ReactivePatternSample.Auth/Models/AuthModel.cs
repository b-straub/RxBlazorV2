using ReactivePatternSample.Status.Models;
using ReactivePatternSample.Storage.Models;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Auth.Models;

/// <summary>
/// Authentication domain model - manages user authentication state.
///
/// Patterns demonstrated:
/// - Singleton scope for persistent auth state
/// - [ObservableComponent] for UI generation
/// - Partial constructor for cross-domain DI (Storage, Status)
/// - [ObservableTrigger] for validation on property change
///
/// File organization:
/// - AuthModel.cs: Constructor and properties
/// - AuthModel.Commands.cs: Commands with their implementations
/// - AuthModel.Triggers.cs: Trigger methods (validation)
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class AuthModel : ObservableModel
{
    /// <summary>
    /// Partial constructor - generator creates DI injection for referenced models.
    /// StorageModel for user validation, AppStatusModel for error reporting.
    /// </summary>
    public partial AuthModel(StorageModel storage, AppStatusModel status);

    /// <summary>
    /// Whether the user is currently authenticated.
    /// </summary>
    [ObservableComponentTrigger]
    public partial bool IsAuthenticated { get; set; }

    /// <summary>
    /// The current authenticated user, if any.
    /// </summary>
    [ObservableComponentTrigger]
    public partial User? CurrentUser { get; set; }

    /// <summary>
    /// Username input field with validation trigger.
    /// </summary>
    [ObservableTrigger(nameof(ValidateUsername))]
    public partial string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password input field (not persisted).
    /// </summary>
    public partial string Password { get; set; } = string.Empty;

    /// <summary>
    /// Validation error message for username.
    /// </summary>
    public partial string? UsernameError { get; set; }

    /// <summary>
    /// General login error message.
    /// </summary>
    public partial string? LoginError { get; set; }

    /// <summary>
    /// Whether a login operation is in progress.
    /// </summary>
    public partial bool IsLoggingIn { get; set; }
}
