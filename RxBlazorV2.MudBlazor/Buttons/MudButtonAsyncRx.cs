using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using RxBlazorV2.Interface;
using RxBlazorV2.MudBlazor.Internal;

namespace RxBlazorV2.MudBlazor.Buttons;

/// <summary>
/// A MudButton that binds to an IObservableCommandAsync with progress and cancellation support.
/// </summary>
public class MudButtonAsyncRx : MudButton
{
    /// <summary>
    /// The async observable command to bind to this button.
    /// </summary>
    [Parameter, EditorRequired]
    public required IObservableCommandAsync Command { get; set; }

    /// <summary>
    /// Optional confirmation function called before execution.
    /// Return true to proceed, false to cancel.
    /// </summary>
    [Parameter]
    public Func<Task<bool>>? ConfirmExecutionAsync { get; set; }

    /// <summary>
    /// Text to display on the cancel button when the command supports cancellation.
    /// If not set, no cancel functionality is shown.
    /// </summary>
    [Parameter]
    public string? CancelText { get; set; }

    /// <summary>
    /// Color for the button when in cancel mode.
    /// </summary>
    [Parameter]
    public Color CancelColor { get; set; } = Color.Warning;

    /// <summary>
    /// Whether to show a progress spinner during execution.
    /// </summary>
    [Parameter]
    public bool HasProgress { get; set; } = true;

    private RenderFragment? _originalContent;
    private Color _originalColor;
    private bool _initialized;

    protected override void OnInitialized()
    {
        _originalContent = ChildContent;
        _originalColor = Color;
        _initialized = true;
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (!_initialized)
        {
            base.OnParametersSet();
            return;
        }

        if (Command.Executing)
        {
            if (CancelText is not null)
            {
                Color = CancelColor;
                ChildContent = ButtonRenderHelper.RenderCancel(_originalContent, CancelText, HasProgress);
                OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => Command.Cancel());
                Disabled = false;
            }
            else
            {
                if (HasProgress)
                {
                    ChildContent = ButtonRenderHelper.RenderProgress(_originalContent);
                }
                Disabled = true;
            }
        }
        else
        {
            ChildContent = _originalContent;
            Color = _originalColor;
            OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, ExecuteCommandAsync);
            Disabled = !Command.CanExecute;
        }

        base.OnParametersSet();
    }

    private async Task ExecuteCommandAsync()
    {
        if (ConfirmExecutionAsync is not null && !await ConfirmExecutionAsync())
        {
            return;
        }

        await Command.ExecuteAsync();
    }
}

/// <summary>
/// A MudButton that binds to a parameterized IObservableCommandAsync with progress and cancellation support.
/// </summary>
/// <typeparam name="T">The command parameter type.</typeparam>
public class MudButtonAsyncRxOf<T> : MudButton
{
    /// <summary>
    /// The async observable command to bind to this button.
    /// </summary>
    [Parameter, EditorRequired]
    public required IObservableCommandAsync<T> Command { get; set; }

    /// <summary>
    /// The parameter to pass to the command.
    /// </summary>
    [Parameter, EditorRequired]
    public required T Parameter { get; set; }
    
    /// <summary>
    /// Optional confirmation function called before execution.
    /// Return true to proceed, false to cancel.
    /// </summary>
    [Parameter]
    public Func<Task<bool>>? ConfirmExecutionAsync { get; set; }

    /// <summary>
    /// Text to display on the cancel button when the command supports cancellation.
    /// If not set, no cancel functionality is shown.
    /// </summary>
    [Parameter]
    public string? CancelText { get; set; }

    /// <summary>
    /// Color for the button when in cancel mode.
    /// </summary>
    [Parameter]
    public Color CancelColor { get; set; } = Color.Warning;

    /// <summary>
    /// Whether to show a progress spinner during execution.
    /// </summary>
    [Parameter]
    public bool HasProgress { get; set; } = true;

    private RenderFragment? _originalContent;
    private Color _originalColor;
    private bool _initialized;

    protected override void OnInitialized()
    {
        _originalContent = ChildContent;
        _originalColor = Color;
        _initialized = true;
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (!_initialized)
        {
            base.OnParametersSet();
            return;
        }

        if (Command.Executing)
        {
            if (CancelText is not null)
            {
                Color = CancelColor;
                ChildContent = ButtonRenderHelper.RenderCancel(_originalContent, CancelText, HasProgress);
                OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => Command.Cancel());
                Disabled = false;
            }
            else
            {
                if (HasProgress)
                {
                    ChildContent = ButtonRenderHelper.RenderProgress(_originalContent);
                }
                Disabled = true;
            }
        }
        else
        {
            ChildContent = _originalContent;
            Color = _originalColor;
            OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, ExecuteCommandAsync);
            Disabled = !Command.CanExecute;
        }

        base.OnParametersSet();
    }

    private async Task ExecuteCommandAsync()
    {
        if (ConfirmExecutionAsync is not null && !await ConfirmExecutionAsync())
        {
            return;
        }

        await Command.ExecuteAsync(Parameter);
    }
}
