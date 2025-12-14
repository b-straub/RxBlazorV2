using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Auth.Models;

/// <summary>
/// Commands and their implementations for AuthModel.
/// </summary>
public partial class AuthModel
{
    /// <summary>
    /// Command to perform login.
    /// </summary>
    [ObservableCommand(nameof(LoginAsync), nameof(CanLogin))]
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
    /// Performs fake login with simulated delay.
    /// </summary>
    private async Task LoginAsync(CancellationToken ct)
    {
        IsLoggingIn = true;
        LoginError = null;

        try
        {
            // Simulate network delay
            await Task.Delay(500, ct);

            // Validate credentials using Storage domain
            var user = Storage.ValidateCredentials(Username, Password);

            if (user is null)
            {
                LoginError = "Invalid username or password";
                Status.AddWarning("Login failed: Invalid credentials", "Auth");
                return;
            }

            CurrentUser = user;
            IsAuthenticated = true;
            Password = string.Empty; // Clear password after successful login

            Status.AddSuccess($"Welcome, {user.DisplayName ?? user.Username}!", "Auth");
        }
        catch (OperationCanceledException)
        {
            // Login was cancelled
        }
        catch (Exception ex)
        {
            LoginError = "An error occurred during login";
            Status.AddError($"Login error: {ex.Message}", "Auth");
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

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
