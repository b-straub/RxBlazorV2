using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using RxBlazorV2.Interface;
using RxBlazorV2.MudBlazor.Internal;

namespace RxBlazorV2.MudBlazor.IconButtons;

/// <summary>
/// A MudIconButton that binds to an IObservableCommandAsync with progress icon support.
/// </summary>
public class MudIconButtonAsyncRx : MudIconButton
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
    /// The icon variant style to use for progress icons.
    /// </summary>
    [Parameter]
    public IconVariant? IconVariant { get; set; }

    /// <summary>
    /// Color for the button when executing.
    /// </summary>
    [Parameter]
    public Color ExecutingColor { get; set; } = Color.Warning;

    /// <summary>
    /// Whether to show a progress icon during execution.
    /// </summary>
    [Parameter]
    public bool HasProgress { get; set; } = true;

    private string? _originalIcon;
    private Color _originalColor;
    private bool _initialized;

    protected override void OnInitialized()
    {
        _initialized = true;
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(Icon))
        {
            throw new InvalidOperationException("Icon is required for MudIconButtonAsyncRx");
        }

        if (!_initialized)
        {
            base.OnParametersSet();
            return;
        }

        // Store original icon on first non-executing pass
        if (!Command.Executing)
        {
            _originalIcon = Icon;
            _originalColor = Color;
        }

        if (Command.Executing)
        {
            if (HasProgress)
            {
                Icon = IconVariant.GetProgressIcon();
            }
            Color = ExecutingColor;
            OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => Command.Cancel());
            Disabled = false;
        }
        else
        {
            Icon = _originalIcon ?? Icon;
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
/// A MudIconButton that binds to a parameterized IObservableCommandAsync with progress icon support.
/// </summary>
/// <typeparam name="T">The command parameter type.</typeparam>
public class MudIconButtonAsyncRxOf<T> : MudIconButton
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
    /// The icon variant style to use for progress icons.
    /// </summary>
    [Parameter]
    public IconVariant? IconVariant { get; set; }

    /// <summary>
    /// Color for the button when executing.
    /// </summary>
    [Parameter]
    public Color ExecutingColor { get; set; } = Color.Warning;

    /// <summary>
    /// Whether to show a progress icon during execution.
    /// </summary>
    [Parameter]
    public bool HasProgress { get; set; } = true;

    private string? _originalIcon;
    private Color _originalColor;
    private bool _initialized;

    protected override void OnInitialized()
    {
        _initialized = true;
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(Icon))
        {
            throw new InvalidOperationException("Icon is required for MudIconButtonAsyncRx<T>");
        }

        if (!_initialized)
        {
            base.OnParametersSet();
            return;
        }

        if (!Command.Executing)
        {
            _originalIcon = Icon;
            _originalColor = Color;
        }

        if (Command.Executing)
        {
            if (HasProgress)
            {
                Icon = IconVariant.GetProgressIcon();
            }
            Color = ExecutingColor;
            OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => Command.Cancel());
            Disabled = false;
        }
        else
        {
            Icon = _originalIcon ?? Icon;
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
