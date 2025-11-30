using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace RxBlazorV2.MudBlazor.Internal;

/// <summary>
/// Helper class for rendering progress and cancel content in buttons.
/// </summary>
internal static class ButtonRenderHelper
{
    /// <summary>
    /// Renders a progress spinner with optional text content.
    /// </summary>
    public static RenderFragment RenderProgress(RenderFragment? originalContent) => builder =>
    {
        builder.OpenComponent<MudProgressCircular>(0);
        builder.AddAttribute(1, "Indeterminate", true);
        builder.AddAttribute(2, "Size", Size.Small);
        builder.AddAttribute(3, "Class", "ms-n1");
        builder.CloseComponent();

        if (originalContent is not null)
        {
            builder.OpenComponent<MudText>(4);
            builder.AddAttribute(5, "Class", "ms-2");
            builder.AddAttribute(6, "ChildContent", originalContent);
            builder.CloseComponent();
        }
    };

    /// <summary>
    /// Renders cancel content with optional progress spinner and text.
    /// </summary>
    public static RenderFragment RenderCancel(
        RenderFragment? originalContent,
        string? cancelText,
        bool hasProgress) => builder =>
    {
        if (hasProgress)
        {
            builder.OpenComponent<MudProgressCircular>(0);
            builder.AddAttribute(1, "Indeterminate", true);
            builder.AddAttribute(2, "Size", Size.Small);
            builder.AddAttribute(3, "Class", "ms-n1");
            builder.CloseComponent();
        }

        builder.OpenComponent<MudText>(4);
        if (hasProgress)
        {
            builder.AddAttribute(5, "Class", "ms-2");
        }

        if (cancelText is not null)
        {
            builder.AddAttribute(6, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.AddContent(7, cancelText);
            }));
        }
        else if (originalContent is not null)
        {
            builder.AddAttribute(6, "ChildContent", originalContent);
        }

        builder.CloseComponent();
    };
}
