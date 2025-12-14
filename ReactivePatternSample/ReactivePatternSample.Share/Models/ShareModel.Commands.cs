using ReactivePatternSample.Settings.Models;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;

namespace ReactivePatternSample.Share.Models;

/// <summary>
/// Commands and their implementations for ShareModel.
/// </summary>
public partial class ShareModel
{
    /// <summary>
    /// Command to copy to clipboard (simulated).
    /// </summary>
    [ObservableCommand(nameof(CopyToClipboardAsync))]
    public partial IObservableCommandAsync CopyCommand { get; }

    /// <summary>
    /// Trigger method for auto-export when dialog opens.
    /// Only exports if dialog is being opened (not closed).
    /// Uses fire-and-forget pattern since trigger is sync.
    /// </summary>
    private void AutoExportOnDialogOpen()
    {
        if (IsDialogOpen)
        {
            _ = PerformExportAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Performs the export operation with the current format from Settings.
    /// </summary>
    internal async Task PerformExportAsync(CancellationToken ct)
    {
        IsExporting = true;
        CopySuccess = false;

        try
        {
            // Simulate processing time
            await Task.Delay(300, ct);

            var format = Settings.PreferredExportFormat;
            ExportedText = format switch
            {
                ExportFormat.Plain => ExportAsPlainText(),
                ExportFormat.Markdown => ExportAsMarkdown(),
                ExportFormat.Json => ExportAsJson(),
                _ => ExportAsPlainText()
            };

            Status.AddSuccess($"Exported {Todo.UserItems.Count} items as {format}", "Share");
        }
        finally
        {
            IsExporting = false;
        }
    }

    /// <summary>
    /// Copies the exported text to clipboard (simulated).
    /// </summary>
    private async Task CopyToClipboardAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(ExportedText))
        {
            Status.AddWarning("Nothing to copy - export first", "Share");
            return;
        }

        // Simulate clipboard operation (in real app, use JS interop)
        await Task.Delay(100, ct);

        CopySuccess = true;
        Status.AddSuccess("Copied to clipboard", "Share");
    }

    /// <summary>
    /// Exports as plain text.
    /// </summary>
    private string ExportAsPlainText()
    {
        var lines = new List<string> { "TODO LIST", new string('=', 40) };

        foreach (var item in Todo.UserItems)
        {
            var status = item.IsCompleted ? "[✓]" : "[ ]";
            lines.Add($"{status} {item.Title}");
            if (!string.IsNullOrEmpty(item.Description))
            {
                lines.Add($"    {item.Description}");
            }
        }

        lines.Add(new string('=', 40));
        lines.Add($"Total: {Todo.UserItems.Count} items ({Todo.CompletedCount} completed)");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Exports as Markdown.
    /// </summary>
    private string ExportAsMarkdown()
    {
        var lines = new List<string> { "# TODO LIST", "" };

        foreach (var item in Todo.UserItems)
        {
            var checkbox = item.IsCompleted ? "[x]" : "[ ]";
            lines.Add($"- {checkbox} **{item.Title}**");
            if (!string.IsNullOrEmpty(item.Description))
            {
                lines.Add($"  - {item.Description}");
            }
        }

        lines.Add("");
        lines.Add($"---");
        lines.Add($"*Total: {Todo.UserItems.Count} items ({Todo.CompletedCount} completed)*");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Exports as JSON.
    /// </summary>
    private string ExportAsJson()
    {
        var items = Todo.UserItems.Select(i => new
        {
            title = i.Title,
            description = i.Description,
            completed = i.IsCompleted,
            created = i.CreatedAt.ToString("O")
        });

        // Simple manual JSON formatting (in real app use System.Text.Json)
        var itemsJson = string.Join(",\n    ", items.Select(i =>
            $"{{ \"title\": \"{EscapeJson(i.title)}\", \"description\": {(i.description is null ? "null" : $"\"{EscapeJson(i.description)}\"")}, \"completed\": {i.completed.ToString().ToLower()}, \"created\": \"{i.created}\" }}"));

        return $"{{\n  \"todoList\": [\n    {itemsJson}\n  ],\n  \"total\": {Todo.UserItems.Count},\n  \"completed\": {Todo.CompletedCount}\n}}";
    }

    private static string EscapeJson(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
}
