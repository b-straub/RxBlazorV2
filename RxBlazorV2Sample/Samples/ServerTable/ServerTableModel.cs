using MudBlazor;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;
using RxBlazorV2Sample.Services;

namespace RxBlazorV2Sample.Samples.ServerTable;

/// <summary>
/// Summary of the most recently completed query. A record, so the generated setter is
/// equality-guarded and an identical outcome publishes nothing.
/// </summary>
public sealed record SearchOutcome(
    string Term,
    bool Highlighted,
    int TotalHits,
    TimeSpan Elapsed,
    bool WasSearch);

/// <summary>
/// Drives a server-side <c>MudTable</c> over a 200,000 document corpus.
///
/// <para>
/// <b>The problem this sample solves.</b> <c>MudTable.ServerData</c> is a pull API: MudBlazor calls
/// it from <c>OnAfterRenderAsync(firstRender)</c> and from <c>ReloadServerData()</c>, and from
/// nowhere else - a re-render never refetches. So a reactive model has to *ask* the table to reload,
/// and the naive ways of doing that are all bad. One <c>[ObservableComponentTriggerAsync]</c> per
/// input means several near-identical hooks that all call <c>ReloadServerData()</c>; collapsing them
/// into a single counter property that gets incremented is the "toggle as a signal" anti-pattern.
/// </para>
///
/// <para>
/// <b>What this model does instead.</b> The two inputs share one
/// <c>[ObservableComponentBatchAsync("search")]</c> batch, which generates exactly one hook,
/// <c>OnSearchBatchChangedAsync</c>, dispatched with <c>AwaitOperation.Switch</c>. The debounce
/// window is per property: the typed term settles for 250 ms, the switch reloads at once. So typing
/// produces one reload per burst rather than one per keystroke, and a newer query releases the
/// previous hook immediately, so its <c>ReloadServerData()</c> call reaches MudTable, whose own
/// <c>CancelToken()</c> aborts the in-flight fetch. No counter, no committed-query record, no
/// manual CancellationTokenSource.
/// </para>
///
/// <para>
/// <b>The one rule for the callback.</b> <see cref="LoadServerDataAsync"/> may publish *results*
/// (<see cref="LastOutcome"/>, <see cref="SampleBaseModel.LogEntries"/>) but never *inputs*. Writing
/// a batch member from inside the callback would ask the table to reload itself.
/// </para>
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ServerTableModel : SampleBaseModel
{
    /// <summary>
    /// Batch id shared by the search inputs. Also the name the generated hook is derived from:
    /// <c>OnSearchBatchChangedAsync</c>.
    /// </summary>
    public const string SearchBatchId = "search";

    /// <summary>
    /// Trailing debounce window for the typed search term. Deliberately shorter than the service's
    /// simulated latency so that a query is usually still in flight when the next one is committed -
    /// which is what makes the Switch cancellation visible in the log rather than merely theoretical.
    /// </summary>
    public const int SearchDebounceMs = 250;

    // Sort labels, shared with the razor so MudTableSortLabel and the parser cannot drift apart.
    public const string TitleSortLabel = "title";
    public const string PublishedSortLabel = "published";
    public const string WordsSortLabel = "words";

    public override string Usage =>
        "[ObservableComponentBatchAsync] collapses several inputs into one hook - with a per-property debounce window - which reloads a server-driven MudTable";

    public partial ServerTableModel(IDocumentSearchService search);

    /// <summary>
    /// Typed input, so it debounces: only the settled term reaches the backend.
    /// </summary>
    [ObservableComponentBatchAsync(SearchBatchId, SearchDebounceMs)]
    public partial string SearchTerm { get; set; } = "";

    /// <summary>
    /// A switch click is a single deliberate action, so it carries no debounce window and reloads
    /// at once. Same batch, same hook - only the timing differs, which is what the per-property
    /// window on <c>[ObservableComponentBatchAsync]</c> is for.
    /// </summary>
    [ObservableComponentBatchAsync(SearchBatchId)]
    public partial bool HighlightMatches { get; set; }

    /// <summary>
    /// Result state published by <see cref="LoadServerDataAsync"/> once a page has been fetched.
    /// Not part of the batch, so writing it can never trigger another reload.
    /// </summary>
    public partial SearchOutcome? LastOutcome { get; set; }

    [ObservableCommand(nameof(Reset), nameof(CanReset))]
    public partial IObservableCommand ResetCommand { get; }

    public int CorpusSize => Search.CorpusSize;

    public TimeSpan SimulatedLatency => Search.SimulatedLatency;

    public bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchTerm);

    /// <summary>
    /// The <c>MudTable.ServerData</c> callback. Paging and sorting arrive in
    /// <paramref name="state"/> - the table owns them - while the term and the highlight mode come
    /// from this model. Neither side duplicates the other.
    ///
    /// <para>
    /// <see cref="OperationCanceledException"/> is deliberately not swallowed. MudTable cancels the
    /// token when a newer reload starts, and letting the exception through leaves the previously
    /// fetched page on screen instead of blanking the table between queries.
    /// </para>
    /// </summary>
    public async Task<TableData<SearchHit>> LoadServerDataAsync(TableState state, CancellationToken cancellationToken)
    {
        var term = SearchTerm.Trim();
        var request = new SearchRequest(
            term,
            HighlightMatches,
            state.Page * state.PageSize,
            state.PageSize,
            ParseSortField(state.SortLabel),
            state.SortDirection == SortDirection.Descending);

        LogEntries.Add(new LogEntry(
            term.Length == 0
                ? $"Browse page {state.Page + 1} ({state.PageSize} rows, sorted by {request.SortField})"
                : $"Query '{term}' started (page {state.Page + 1}, highlight {(HighlightMatches ? "on" : "off")})",
            DateTime.Now));

        try
        {
            var page = await Search.SearchAsync(request, cancellationToken);

            LastOutcome = new SearchOutcome(term, HighlightMatches, page.TotalHits, page.Elapsed, page.WasSearch);

            if (page.WasSearch)
            {
                LogEntries.Add(new LogEntry(
                    $"Query '{term}' returned {page.TotalHits:N0} hits in {page.Elapsed.TotalMilliseconds:F0} ms",
                    DateTime.Now));
            }

            return new TableData<SearchHit> { Items = page.Hits, TotalItems = page.TotalHits };
        }
        catch (OperationCanceledException)
        {
            LogEntries.Add(new LogEntry(
                $"Query '{term}' cancelled - superseded by a newer query",
                DateTime.Now));
            throw;
        }
        catch (Exception ex)
        {
            // A genuine failure, unlike the cancellation above. Letting this escape would leave
            // MudTable stuck with Loading = true, so the callback absorbs it and returns an empty
            // page. Note this is *not* the "wrap the exception to prefix its message" anti-pattern:
            // ServerData is a callback the table invokes, not an [ObservableCommand], so there is no
            // command error formatter in play here.
            LogEntries.Add(new LogEntry($"Query '{term}' failed: {ex.Message}", DateTime.Now));
            LastOutcome = new SearchOutcome(term, HighlightMatches, 0, TimeSpan.Zero, WasSearch: true);
            return new TableData<SearchHit> { Items = [], TotalItems = 0 };
        }
    }

    /// <summary>
    /// Clears both inputs. Because they sit in the same batch with different windows, the log shows
    /// the honest consequence: the switch reloads immediately, and the debounced term supersedes
    /// that reload 250 ms later. <c>AwaitOperation.Switch</c> cancels the first fetch cleanly, so
    /// this costs one aborted query rather than a stale page.
    /// </summary>
    private void Reset()
    {
        SearchTerm = string.Empty;
        HighlightMatches = false;

        LogEntries.Add(new LogEntry("Reset search inputs", DateTime.Now));
    }

    private bool CanReset() => HasActiveSearch || HighlightMatches;

    private static DocumentSortField ParseSortField(string? sortLabel) => sortLabel switch
    {
        TitleSortLabel => DocumentSortField.TITLE,
        PublishedSortLabel => DocumentSortField.PUBLISHED,
        WordsSortLabel => DocumentSortField.WORDS,
        _ => DocumentSortField.RELEVANCE
    };
}
