using System.Diagnostics;
using System.Net;
using System.Text;

namespace RxBlazorV2Sample.Services;

/// <summary>
/// Column the caller wants the result set ordered by. Maps onto <c>TableState.SortLabel</c>.
/// </summary>
public enum DocumentSortField
{
    /// <summary>Number of matched terms, highest first. Falls back to corpus order when not searching.</summary>
    RELEVANCE,
    TITLE,
    PUBLISHED,
    WORDS
}

/// <summary>
/// One page request. Paging and sorting come from the table, term and highlighting from the model.
/// </summary>
public sealed record SearchRequest(
    string Term,
    bool HighlightMatches,
    int Skip,
    int Take,
    DocumentSortField SortField,
    bool Descending);

/// <summary>
/// One materialised row. <see cref="TitleMarkup"/> and <see cref="BodyMarkup"/> are only populated
/// when the request asked for highlighting, so a row carries its own presentation and the UI needs
/// no side lookup table.
/// </summary>
public sealed record SearchHit(
    int Id,
    string Title,
    string Body,
    string Category,
    DateOnly Published,
    int Words,
    int Score,
    string? TitleMarkup,
    string? BodyMarkup);

/// <summary>
/// A page of results plus the totals the pager and the result summary need.
/// </summary>
public sealed record SearchPage(
    IReadOnlyList<SearchHit> Hits,
    int TotalHits,
    TimeSpan Elapsed,
    bool WasSearch);

/// <summary>
/// Read side of the demo corpus. Modelled on a real search server: it owns an index, answers a
/// page at a time, and honours the caller's cancellation token.
/// </summary>
public interface IDocumentSearchService
{
    /// <summary>Total number of documents in the corpus.</summary>
    int CorpusSize { get; }

    /// <summary>Simulated round-trip latency applied to searches (not to plain browsing).</summary>
    TimeSpan SimulatedLatency { get; }

    /// <summary>
    /// Returns one page of results. Browsing (empty term) completes synchronously; searching awaits
    /// the simulated round-trip, which is where <paramref name="cancellationToken"/> takes effect.
    /// </summary>
    ValueTask<SearchPage> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// In-memory stand-in for a full-text search backend over 200,000 documents.
///
/// <para>
/// <b>Why it is built this way.</b> Storing 200,000 materialised title/body strings would cost tens
/// of megabytes in WASM and seconds of start-up. Instead each document is a ~20 byte struct holding
/// indices into small shared word pools, so the whole corpus is one flat array of a few megabytes,
/// and text is materialised only for the rows on the page actually being returned. Matching runs
/// against the pools (a few hundred short strings) rather than against 200,000 documents - the same
/// trick an inverted index plays, and the reason a real FTS engine is fast.
/// </para>
///
/// <para>
/// Ordering uses index arrays precomputed once at construction, so paging a sorted result set is a
/// single linear pass with no comparison sort per query.
/// </para>
/// </summary>
public sealed class DocumentSearchService : IDocumentSearchService
{
    private const int CorpusCount = 200_000;
    private const int CorpusSeed = 20260906;
    private const int SnippetRadius = 45;

    /// <summary>
    /// Simulated server round-trip. Deliberately longer than the sample's debounce window so that a
    /// query is still in flight when the next one is committed - that is the case where cancellation
    /// is observable rather than theoretical.
    /// </summary>
    private static readonly TimeSpan SearchLatency = TimeSpan.FromMilliseconds(600);

    // Word pools. Sorted with StringComparer.Ordinal at construction so that ordering documents by
    // the (subject, topic, qualifier) index tuple is exactly ordinal title order: the separator is a
    // space (0x20), which sorts before every letter used here, so prefix ordering stays consistent.
    private readonly string[] _subjects;
    private readonly string[] _topics;
    private readonly string[] _qualifiers;
    private readonly string[] _sentences;
    private readonly string[] _categories;
    private readonly int[] _sentenceWordCounts;

    private readonly DocumentRecord[] _corpus;

    // Precomputed ascending orders, one per sortable column.
    private readonly int[] _orderByTitle;
    private readonly int[] _orderByPublished;
    private readonly int[] _orderByWords;

    private static readonly DateOnly CorpusEndDate = new(2026, 9, 6);

    public DocumentSearchService()
    {
        _subjects = Sorted(SubjectPool);
        _topics = Sorted(TopicPool);
        _qualifiers = Sorted(QualifierPool);
        _sentences = Sorted(SentencePool);
        _categories = Sorted(CategoryPool);

        _sentenceWordCounts = _sentences.Select(CountWords).ToArray();

        _corpus = BuildCorpus();
        _orderByTitle = BuildOrder(document => ((document.Subject * _topics.Length) + document.Topic) * _qualifiers.Length + document.Qualifier);
        _orderByPublished = BuildOrder(document => document.DayOffset);
        _orderByWords = BuildOrder(WordCount);
    }

    public int CorpusSize => _corpus.Length;

    public TimeSpan SimulatedLatency => SearchLatency;

    /// <inheritdoc />
    public ValueTask<SearchPage> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Term))
        {
            // Browsing hits no backend, so there is nothing to await and nothing to cancel.
            // This is the synchronous half that makes ValueTask the right return type here.
            return new ValueTask<SearchPage>(BuildPage(request, term: null));
        }

        return SearchWithLatencyAsync(request, cancellationToken);
    }

    private async ValueTask<SearchPage> SearchWithLatencyAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // The only cancellation point. MudTable cancels this token when a newer ReloadServerData
        // starts, so a superseded query dies here instead of returning a stale page.
        await Task.Delay(SearchLatency, cancellationToken);

        var page = BuildPage(request, request.Term.Trim());
        stopwatch.Stop();

        return page with { Elapsed = stopwatch.Elapsed };
    }

    /// <summary>
    /// Single linear pass over the corpus in the requested order: counts every match for the pager
    /// and materialises only the rows inside the requested window.
    /// </summary>
    private SearchPage BuildPage(SearchRequest request, string? term)
    {
        var matcher = term is null ? null : new TermMatcher(this, term);

        return request.SortField == DocumentSortField.RELEVANCE && matcher is not null
            ? BuildRelevancePage(request, term!, matcher)
            : BuildOrderedPage(request, term, matcher);
    }

    private SearchPage BuildOrderedPage(SearchRequest request, string? term, TermMatcher? matcher)
    {
        var order = OrderFor(request.SortField);
        var hits = new List<SearchHit>(Math.Min(request.Take, 256));
        var windowEnd = request.Skip + request.Take;
        var rank = 0;

        for (var position = 0; position < order.Length; position++)
        {
            // Descending simply walks the same precomputed ascending order backwards.
            var index = order[request.Descending ? order.Length - 1 - position : position];
            var document = _corpus[index];

            var score = matcher?.Score(document) ?? 1;
            if (score == 0)
            {
                continue;
            }

            if (rank >= request.Skip && rank < windowEnd)
            {
                hits.Add(Materialise(document, score, term, request.HighlightMatches));
            }

            rank++;
        }

        return new SearchPage(hits, rank, Elapsed: TimeSpan.Zero, WasSearch: term is not null);
    }

    /// <summary>
    /// Relevance order cannot be precomputed because the score depends on the term. Scores are small
    /// integers, so the ordering is a bucket sort: one pass to size the buckets, one to fill the
    /// window - still linear, and still without materialising anything outside the page.
    /// </summary>
    private SearchPage BuildRelevancePage(SearchRequest request, string term, TermMatcher matcher)
    {
        var bucketCount = TermMatcher.MaxScore + 1;
        var counts = new int[bucketCount];

        foreach (var document in _corpus)
        {
            var score = matcher.Score(document);
            if (score > 0)
            {
                counts[score]++;
            }
        }

        // Rank offsets. Ascending on a relevance column means "best first", so the highest bucket
        // takes rank 0; flipping the sort direction walks the buckets the other way.
        var offsets = new int[bucketCount];
        var running = 0;
        for (var step = 0; step < bucketCount; step++)
        {
            var score = request.Descending ? step : TermMatcher.MaxScore - step;
            if (score <= 0)
            {
                continue;
            }

            offsets[score] = running;
            running += counts[score];
        }

        var total = running;
        var hits = new SearchHit?[request.Take];
        var seen = new int[bucketCount];
        var windowEnd = request.Skip + request.Take;

        foreach (var document in _corpus)
        {
            var score = matcher.Score(document);
            if (score == 0)
            {
                continue;
            }

            var rank = offsets[score] + seen[score];
            seen[score]++;

            if (rank >= request.Skip && rank < windowEnd)
            {
                hits[rank - request.Skip] = Materialise(document, score, term, request.HighlightMatches);
            }
        }

        var page = hits.Where(hit => hit is not null).Select(hit => hit!).ToList();
        return new SearchPage(page, total, Elapsed: TimeSpan.Zero, WasSearch: true);
    }

    private int[] OrderFor(DocumentSortField sortField) => sortField switch
    {
        DocumentSortField.TITLE => _orderByTitle,
        DocumentSortField.PUBLISHED => _orderByPublished,
        DocumentSortField.WORDS => _orderByWords,
        // Relevance without a term has no meaningful score, so newest-first stands in for it.
        _ => _orderByPublished
    };

    /// <summary>
    /// Turns a compact record into a display row. This is the only place strings are built, and it
    /// runs at most <c>Take</c> times per query.
    /// </summary>
    private SearchHit Materialise(DocumentRecord document, int score, string? term, bool highlight)
    {
        var title = BuildTitle(document);
        var body = BuildBody(document);
        var published = CorpusEndDate.AddDays(-document.DayOffset);

        string? titleMarkup = null;
        string? bodyMarkup = null;

        if (highlight && term is not null)
        {
            titleMarkup = HighlightAll(title, term);
            bodyMarkup = HighlightSnippet(body, term);
        }

        return new SearchHit(
            document.Id,
            title,
            body,
            _categories[document.Category],
            published,
            WordCount(document),
            score,
            titleMarkup,
            bodyMarkup);
    }

    private string BuildTitle(DocumentRecord document) =>
        $"{_subjects[document.Subject]} {_topics[document.Topic]} {_qualifiers[document.Qualifier]}";

    private string BuildBody(DocumentRecord document) =>
        $"{_sentences[document.SentenceA]} {_sentences[document.SentenceB]} {_sentences[document.SentenceC]}";

    private int WordCount(DocumentRecord document) =>
        3 +
        _sentenceWordCounts[document.SentenceA] +
        _sentenceWordCounts[document.SentenceB] +
        _sentenceWordCounts[document.SentenceC];

    /// <summary>
    /// Wraps every occurrence of <paramref name="term"/> in <c>&lt;mark&gt;</c>. The source text is
    /// HTML-encoded segment by segment before the tags go in, so the result is safe to render as a
    /// <see cref="Microsoft.AspNetCore.Components.MarkupString"/>.
    /// </summary>
    private static string HighlightAll(string plain, string term)
    {
        var builder = new StringBuilder(plain.Length + 32);
        var cursor = 0;

        while (true)
        {
            var found = plain.IndexOf(term, cursor, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                builder.Append(WebUtility.HtmlEncode(plain[cursor..]));
                return builder.ToString();
            }

            builder.Append(WebUtility.HtmlEncode(plain[cursor..found]));
            builder.Append("<mark>");
            builder.Append(WebUtility.HtmlEncode(plain.Substring(found, term.Length)));
            builder.Append("</mark>");
            cursor = found + term.Length;
        }
    }

    /// <summary>
    /// Highlighted excerpt around the first occurrence, the way FTS5's <c>snippet()</c> behaves.
    /// Falls back to the encoded head of the text when the term is not in the body.
    /// </summary>
    private static string HighlightSnippet(string plain, string term)
    {
        var found = plain.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (found < 0)
        {
            var head = plain.Length <= SnippetRadius * 2 ? plain : plain[..(SnippetRadius * 2)];
            return WebUtility.HtmlEncode(head) + (head.Length < plain.Length ? "&#8230;" : string.Empty);
        }

        var start = Math.Max(0, found - SnippetRadius);
        var end = Math.Min(plain.Length, found + term.Length + SnippetRadius);
        var excerpt = HighlightAll(plain[start..end], term);

        var prefix = start > 0 ? "&#8230;" : string.Empty;
        var suffix = end < plain.Length ? "&#8230;" : string.Empty;
        return prefix + excerpt + suffix;
    }

    private DocumentRecord[] BuildCorpus()
    {
        // Seeded so every run - and every reader of this sample - sees the same corpus.
        var random = new Random(CorpusSeed);
        var corpus = new DocumentRecord[CorpusCount];

        for (var index = 0; index < corpus.Length; index++)
        {
            corpus[index] = new DocumentRecord(
                index + 1,
                (ushort)random.Next(_subjects.Length),
                (ushort)random.Next(_topics.Length),
                (ushort)random.Next(_qualifiers.Length),
                (ushort)random.Next(_sentences.Length),
                (ushort)random.Next(_sentences.Length),
                (ushort)random.Next(_sentences.Length),
                (byte)random.Next(_categories.Length),
                (ushort)random.Next(3650));
        }

        return corpus;
    }

    /// <summary>
    /// Builds an ascending index order from an integer key, using key-based
    /// <see cref="Array.Sort{TKey,TValue}(TKey[],TValue[])"/> rather than a comparison delegate.
    /// </summary>
    private int[] BuildOrder(Func<DocumentRecord, int> keySelector)
    {
        var keys = new int[_corpus.Length];
        var order = new int[_corpus.Length];

        for (var index = 0; index < _corpus.Length; index++)
        {
            keys[index] = keySelector(_corpus[index]);
            order[index] = index;
        }

        Array.Sort(keys, order);
        return order;
    }

    private static string[] Sorted(string[] pool)
    {
        var sorted = (string[])pool.Clone();
        Array.Sort(sorted, StringComparer.Ordinal);
        return sorted;
    }

    private static int CountWords(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>
    /// A document as stored: indices into the word pools plus a publication day offset.
    /// Roughly 20 bytes, held in one flat array - the whole 200,000 document corpus is a few MB.
    /// </summary>
    private readonly record struct DocumentRecord(
        int Id,
        ushort Subject,
        ushort Topic,
        ushort Qualifier,
        ushort SentenceA,
        ushort SentenceB,
        ushort SentenceC,
        byte Category,
        ushort DayOffset);

    /// <summary>
    /// Precomputed per-pool hit masks for one search term - the sample's stand-in for an inverted
    /// index. Built by scanning a few hundred pool entries, after which scoring a document is six
    /// array lookups instead of a substring search over its text.
    /// </summary>
    private sealed class TermMatcher
    {
        internal const int MaxScore = 6;

        private readonly bool[] _subjectHits;
        private readonly bool[] _topicHits;
        private readonly bool[] _qualifierHits;
        private readonly bool[] _sentenceHits;
        private readonly bool[] _categoryHits;

        internal TermMatcher(DocumentSearchService service, string term)
        {
            _subjectHits = Mask(service._subjects, term);
            _topicHits = Mask(service._topics, term);
            _qualifierHits = Mask(service._qualifiers, term);
            _sentenceHits = Mask(service._sentences, term);
            _categoryHits = Mask(service._categories, term);
        }

        /// <summary>
        /// Number of distinct fields matching the term, 0 when the document does not match at all.
        /// </summary>
        internal int Score(DocumentRecord document)
        {
            var score = 0;

            if (_subjectHits[document.Subject])
            {
                score++;
            }

            if (_topicHits[document.Topic])
            {
                score++;
            }

            if (_qualifierHits[document.Qualifier])
            {
                score++;
            }

            if (_categoryHits[document.Category])
            {
                score++;
            }

            if (_sentenceHits[document.SentenceA] ||
                _sentenceHits[document.SentenceB] ||
                _sentenceHits[document.SentenceC])
            {
                score += 2;
            }

            return score;
        }

        private static bool[] Mask(string[] pool, string term)
        {
            var mask = new bool[pool.Length];
            for (var index = 0; index < pool.Length; index++)
            {
                mask[index] = pool[index].Contains(term, StringComparison.OrdinalIgnoreCase);
            }

            return mask;
        }
    }

    private static readonly string[] SubjectPool =
    [
        "Adaptive", "Ambient", "Asynchronous", "Atomic", "Autonomous", "Balanced", "Batched", "Buffered",
        "Cascading", "Cohesive", "Compacted", "Concurrent", "Contextual", "Declarative", "Deferred", "Delegated",
        "Deterministic", "Distributed", "Elastic", "Encrypted", "Ephemeral", "Eventual", "Federated", "Granular",
        "Hierarchical", "Idempotent", "Immutable", "Incremental", "Isolated", "Iterative", "Layered", "Lazy",
        "Managed", "Metered", "Modular", "Nested", "Observable", "Optimistic", "Partitioned", "Persistent",
        "Pipelined", "Reactive", "Redundant", "Replicated", "Resilient", "Sharded", "Streaming", "Versioned"
    ];

    private static readonly string[] TopicPool =
    [
        "aggregation", "allocator", "backpressure", "batch", "benchmark", "bootstrap", "broker", "buffer",
        "cache", "checkpoint", "cluster", "codec", "collector", "compaction", "compiler", "connector",
        "consensus", "container", "coordinator", "cursor", "dashboard", "dispatcher", "encoder", "endpoint",
        "envelope", "executor", "fallback", "filter", "firewall", "gateway", "graph", "handler",
        "handshake", "heartbeat", "index", "ingestion", "interceptor", "journal", "keystore", "ledger",
        "listener", "loader", "manifest", "mapper", "materialiser", "mesh", "middleware", "migration",
        "monitor", "mutation", "namespace", "negotiator", "notifier", "observer", "orchestrator", "partition",
        "pipeline", "planner", "pool", "predicate", "projection", "protocol", "provider", "proxy",
        "publisher", "queue", "quota", "reconciler", "recorder", "reducer", "registry", "replica",
        "repository", "resolver", "retry", "router", "runtime", "sampler", "scheduler", "scanner",
        "schema", "selector", "sequencer", "serialiser", "session", "shard", "snapshot", "socket",
        "splitter", "stream", "subscriber", "supervisor", "throttle", "tracer", "transformer", "validator"
    ];

    private static readonly string[] QualifierPool =
    [
        "adapter", "advisory", "algorithm", "analysis", "annotation", "audit", "baseline", "blueprint",
        "budget", "changelog", "charter", "checklist", "contract", "digest", "dossier", "draft",
        "escalation", "evaluation", "experiment", "guideline", "handbook", "heuristic", "incident", "inventory",
        "matrix", "memo", "metric", "notebook", "outline", "overview", "playbook", "policy",
        "postmortem", "primer", "profile", "proposal", "protocol", "readout", "reference", "report",
        "retrospective", "review", "roadmap", "rubric", "specification", "summary", "survey", "walkthrough"
    ];

    private static readonly string[] CategoryPool =
    [
        "Architecture", "Data", "Networking", "Observability", "Platform", "Reliability", "Security", "Tooling"
    ];

    private static readonly string[] SentencePool =
    [
        "A cold start replays the journal from the last durable checkpoint before accepting traffic.",
        "A partition that falls behind is fenced off until it catches up with the leader.",
        "A retry budget bounds how much redundant work a degraded dependency can cause.",
        "Backpressure travels upstream so the slowest consumer sets the pace for the pipeline.",
        "Batching amortises the fixed cost of a round trip across many small operations.",
        "Cache invalidation is driven by version stamps rather than wall-clock expiry.",
        "Compaction rewrites the log in the background so reads stay proportional to live data.",
        "Concurrent writers converge because every mutation carries a monotonic sequence number.",
        "Deterministic replay makes an incident reproducible long after the traffic has gone.",
        "Each shard owns its own write path and never blocks on a neighbour.",
        "Eventual consistency is acceptable here because every read is idempotent.",
        "Failed deliveries move to a dead letter queue instead of blocking the stream.",
        "Hot keys are detected by sampling and then spread across additional replicas.",
        "Idempotency keys let a client retry safely without duplicating the effect.",
        "Immutable snapshots let readers work without taking a lock on the writer.",
        "Incremental checkpoints keep recovery time flat as the dataset grows.",
        "Latency is measured at the tail because the average hides the failures that matter.",
        "Lazy materialisation defers the expensive projection until a caller actually reads it.",
        "Leases expire automatically so a crashed owner cannot hold a resource forever.",
        "Metrics are aggregated at the edge to keep cardinality under control.",
        "Migrations run forward only, with a compatibility window covering both schemas.",
        "Observability is built in from the start rather than bolted on after an outage.",
        "Optimistic concurrency turns a conflict into a retry instead of a wait.",
        "Ordering is guaranteed per key, never across the whole topic.",
        "Partial failures are surfaced to the caller rather than silently swallowed.",
        "Quotas are enforced per tenant so one workload cannot starve the rest.",
        "Read replicas serve historical queries while the primary handles writes.",
        "Reconciliation loops converge the observed state onto the declared state.",
        "Requests carry a trace identifier that survives every hop in the system.",
        "Retries use exponential backoff with jitter to avoid synchronised stampedes.",
        "Schema changes are additive so old and new readers can coexist.",
        "Secrets are rotated on a schedule and never written to the audit log.",
        "Sequential scans are avoided by keeping a covering index on the hot path.",
        "Shadow traffic validates the new implementation before it takes real load.",
        "Slow consumers are disconnected rather than allowed to grow the buffer without bound.",
        "Snapshots are content addressed so an identical state is stored only once.",
        "State transitions are logged before they are applied, never after.",
        "Streaming results let the client start rendering before the query finishes.",
        "The circuit opens after a threshold of failures and probes periodically to recover.",
        "The coordinator holds no durable state, so any instance can take over.",
        "The index is rebuilt online and swapped in atomically when it is complete.",
        "The scheduler prefers work that unblocks other work over work that is merely old.",
        "Throughput is bounded by the slowest stage, so the pipeline is sized around it.",
        "Timeouts are budgeted end to end rather than chosen independently per hop.",
        "Validation happens at the boundary so internal code can assume well-formed input.",
        "Warm caches are preserved across deployments to avoid a thundering herd.",
        "Work is idempotent by construction, which makes at-least-once delivery sufficient.",
        "Writes are acknowledged only after they are durable on a quorum of replicas."
    ];
}
