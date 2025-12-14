namespace ReactivePatternSample.Auth.Models;

/// <summary>
/// Trigger methods for AuthModel.
/// These methods are automatically called when their associated properties change.
/// </summary>
public partial class AuthModel
{
    /// <summary>
    /// Validates username on change (triggered by [ObservableTrigger]).
    /// </summary>
    private void ValidateUsername()
    {
        UsernameError = string.IsNullOrWhiteSpace(Username)
            ? "Username is required"
            : null;
    }
}
