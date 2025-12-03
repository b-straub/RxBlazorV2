using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using RxBlazorV2.Interface;
using RxBlazorV2.MudBlazor.Internal;

namespace RxBlazorV2.MudBlazor.FabButtons;

/// <summary>
/// A MudFab that binds to an IObservableCommandAsync with progress icon support.
/// </summary>
public class MudFabAsyncRx : MudFab
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
    /// Text to display on the FAB when cancelling.
    /// </summary>
    [Parameter]
    public string? CancelText { get; set; }

    /// <summary>
    /// Color for the FAB when in cancel mode.
    /// </summary>
    [Parameter]
    public Color CancelColor { get; set; } = Color.Warning;

    /// <summary>
    /// Whether to show a progress icon during execution.
    /// </summary>
    [Parameter]
    public bool HasProgress { get; set; } = true;

    private string? _originalStartIcon;
    private string? _originalEndIcon;
    private string? _originalLabel;
    private Color _originalColor;
    private bool _initialized;

    protected override void OnInitialized()
    {
        _originalStartIcon = StartIcon;
        _originalEndIcon = EndIcon;
        _originalLabel = Label;
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
            Color = CancelColor;

            if (HasProgress)
            {
                var progressIcon = IconVariant.GetProgressIcon();

                if (!string.IsNullOrEmpty(_originalStartIcon) && !string.IsNullOrEmpty(_originalEndIcon))
                {
                    EndIcon = progressIcon;
                }
                else if (!string.IsNullOrEmpty(_originalStartIcon))
                {
                    StartIcon = progressIcon;
                }
                else if (!string.IsNullOrEmpty(_originalEndIcon))
                {
                    EndIcon = progressIcon;
                }
            }

            if (CancelText is not null)
            {
                Label = CancelText;
            }

            OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => Command.Cancel());
            Disabled = false;
        }
        else
        {
            StartIcon = _originalStartIcon;
            EndIcon = _originalEndIcon;
            Label = _originalLabel;
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
/// A MudFab that binds to a parameterized IObservableCommandAsync with progress icon support.
/// </summary>
/// <typeparam name="T">The command parameter type.</typeparam>
public class MudFabAsyncRxOf<T> : MudFab
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
    /// Text to display on the FAB when cancelling.
    /// </summary>
    [Parameter]
    public string? CancelText { get; set; }

    /// <summary>
    /// Color for the FAB when in cancel mode.
    /// </summary>
    [Parameter]
    public Color CancelColor { get; set; } = Color.Warning;

    /// <summary>
    /// Whether to show a progress icon during execution.
    /// </summary>
    [Parameter]
    public bool HasProgress { get; set; } = true;

    private string? _originalStartIcon;
    private string? _originalEndIcon;
    private string? _originalLabel;
    private Color _originalColor;
    private bool _initialized;

    protected override void OnInitialized()
    {
        _originalStartIcon = StartIcon;
        _originalEndIcon = EndIcon;
        _originalLabel = Label;
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
            Color = CancelColor;

            if (HasProgress)
            {
                var progressIcon = IconVariant.GetProgressIcon();

                if (!string.IsNullOrEmpty(_originalStartIcon) && !string.IsNullOrEmpty(_originalEndIcon))
                {
                    EndIcon = progressIcon;
                }
                else if (!string.IsNullOrEmpty(_originalStartIcon))
                {
                    StartIcon = progressIcon;
                }
                else if (!string.IsNullOrEmpty(_originalEndIcon))
                {
                    EndIcon = progressIcon;
                }
            }

            if (CancelText is not null)
            {
                Label = CancelText;
            }

            OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => Command.Cancel());
            Disabled = false;
        }
        else
        {
            StartIcon = _originalStartIcon;
            EndIcon = _originalEndIcon;
            Label = _originalLabel;
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
