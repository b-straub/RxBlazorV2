using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using RxBlazorV2.Interface;

namespace RxBlazorV2.MudBlazor.Buttons;

/// <summary>
/// A MudButton that binds to an IObservableCommand.
/// </summary>
public class MudButtonRx : MudButton
{
    /// <summary>
    /// The observable command to bind to this button.
    /// </summary>
    [Parameter, EditorRequired]
    public required IObservableCommand Command { get; set; }

    /// <summary>
    /// Additional guard function to determine if the command can execute.
    /// </summary>
    [Parameter]
    public Func<bool>? CanExecute { get; set; }

    /// <summary>
    /// Optional confirmation function called before execution.
    /// Return true to proceed, false to cancel.
    /// </summary>
    [Parameter]
    public Func<Task<bool>>? ConfirmExecutionAsync { get; set; }

    protected override void OnParametersSet()
    {
        Disabled = !Command.CanExecute || (CanExecute is not null && !CanExecute());
        OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, ExecuteCommandAsync);

        base.OnParametersSet();
    }

    private async Task ExecuteCommandAsync()
    {
        if (ConfirmExecutionAsync is not null && !await ConfirmExecutionAsync())
        {
            return;
        }

        Command.Execute();
    }
}

/// <summary>
/// A MudButton that binds to a parameterized IObservableCommand.
/// </summary>
/// <typeparam name="T">The command parameter type.</typeparam>
public class MudButtonRxOf<T> : MudButton
{
    /// <summary>
    /// The observable command to bind to this button.
    /// </summary>
    [Parameter, EditorRequired]
    public required IObservableCommand<T> Command { get; set; }

    /// <summary>
    /// The parameter to pass to the command.
    /// </summary>
    [Parameter, EditorRequired]
    public required T Parameter { get; set; }

    /// <summary>
    /// Additional guard function to determine if the command can execute.
    /// </summary>
    [Parameter]
    public Func<bool>? CanExecute { get; set; }

    /// <summary>
    /// Optional confirmation function called before execution.
    /// Return true to proceed, false to cancel.
    /// </summary>
    [Parameter]
    public Func<Task<bool>>? ConfirmExecutionAsync { get; set; }

    protected override void OnParametersSet()
    {
        Disabled = !Command.CanExecute || (CanExecute is not null && !CanExecute());
        OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, ExecuteCommandAsync);

        base.OnParametersSet();
    }

    private async Task ExecuteCommandAsync()
    {
        if (ConfirmExecutionAsync is not null && !await ConfirmExecutionAsync())
        {
            return;
        }

        Command.Execute(Parameter);
    }
}
