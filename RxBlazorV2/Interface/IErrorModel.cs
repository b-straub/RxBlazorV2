namespace RxBlazorV2.Interface;

/// <summary>
/// Interface for centralized error handling in ObservableModels.
/// When an ObservableModel injects IErrorModel, command errors are automatically
/// delegated to this model and reset on the original command.
/// </summary>
public interface IErrorModel : IObservableModel
{
    /// <summary>
    /// Called when a command execution throws an exception.
    /// The implementation should store or process the error.
    /// After this method returns, the command's error will be reset.
    /// </summary>
    /// <param name="error">The exception that was thrown during command execution.</param>
    void HandleError(Exception error);
}
