using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Auth.Models;

/// <summary>
/// Thrown by <see cref="AuthModel.LoginAsync"/> when <c>Storage.ValidateCredentials</c>
/// rejects the supplied username / password. Caught by the cancelable command factory
/// and run through <see cref="AuthModel.FormatLoginError"/> for the user-facing message.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid username or password") { }
}

/// <summary>
/// Commands and their implementations for AuthModel.
/// </summary>
public partial class AuthModel
{
    /// <summary>
    /// Command to perform login. Unexpected exceptions are auto-captured by the cancelable factory
    /// and routed through <see cref="FormatLoginError"/> to the configured <c>StatusBaseModel</c>;
    /// no manual try/catch is needed in <see cref="LoginAsync"/>.
    /// </summary>
    [ObservableCommand(nameof(LoginAsync), nameof(CanLogin), nameof(FormatLoginError))]
    public partial IObservableCommandAsync LoginCommand { get; }

    /// <summary>
    /// Command to perform logout.
    /// </summary>
    [ObservableCommand(nameof(Logout), nameof(CanLogout))]
    public partial IObservableCommand LogoutCommand { get; }

    /// <summary>
    /// Can login when username and password are provided and not already authenticated.
    /// </summary>
    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !IsAuthenticated &&
        !IsLoggingIn;

    /// <summary>
    /// Can logout when authenticated.
    /// </summary>
    private bool CanLogout() => IsAuthenticated;

    /// <summary>
    /// Performs fake login with simulated delay. Both the expected "invalid credentials" outcome
    /// and any unexpected error propagate as exceptions to the cancelable command factory, which
    /// records them through <see cref="FormatLoginError"/>. Cancellation is also handled by the
    /// factory and never surfaces here.
    /// </summary>
    private async Task LoginAsync(CancellationToken ct)
    {
        IsLoggingIn = true;

        try
        {
            await Task.Delay(500, ct);

            var user = Storage.ValidateCredentials(Username, Password)
                       ?? throw new InvalidCredentialsException();

            CurrentUser = user;
            IsAuthenticated = true;
            Password = string.Empty;

            Status.AddSuccess($"Welcome, {user.DisplayName ?? user.Username}!", "Auth");
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    /// <summary>
    /// Maps login exceptions to the user-facing text shown by the configured StatusBaseModel.
    /// Cancellation is intercepted by the cancelable factory before this method is invoked, so
    /// only domain-level credential failures and unexpected errors reach this switch.
    /// </summary>
    private string FormatLoginError(Exception ex) => ex switch
    {
        InvalidCredentialsException => "Login failed: Invalid username or password",
        _                           => $"Login error: {ex.Message}",
    };

    /// <summary>
    /// Performs logout.
    /// </summary>
    private void Logout()
    {
        var userName = CurrentUser?.DisplayName ?? CurrentUser?.Username ?? "User";

        CurrentUser = null;
        IsAuthenticated = false;
        Username = string.Empty;
        Password = string.Empty;
        LoginError = null;

        Status.AddInfo($"{userName} logged out", "Auth");
    }
}
