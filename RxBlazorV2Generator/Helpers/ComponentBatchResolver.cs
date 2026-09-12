using Microsoft.CodeAnalysis.CSharp;

namespace RxBlazorV2Generator.Helpers;

/// <summary>
/// Why an <c>[ObservableComponentBatchAsync]</c> batch cannot be turned into a component hook.
/// </summary>
public enum ComponentBatchIssue
{
    /// <summary>The batch is well formed.</summary>
    NONE,

    /// <summary>The batch id cannot be turned into a hook method name.</summary>
    INVALID_BATCH_ID,

    /// <summary>A member declares a negative debounce window.</summary>
    NEGATIVE_DEBOUNCE
}

/// <summary>
/// One property's membership in a batch, with its own debounce window.
/// </summary>
/// <param name="propertyName">Unqualified property name.</param>
/// <param name="debounceMilliseconds">Trailing debounce window; zero means immediate.</param>
public sealed class ComponentBatchMember(string propertyName, int debounceMilliseconds)
{
    public string PropertyName { get; } = propertyName;
    public int DebounceMilliseconds { get; } = debounceMilliseconds;
}

/// <summary>
/// One resolved batch: the members that feed it and the problem found, if any.
/// </summary>
public sealed class ComponentBatchResolution(
    string batchId,
    List<ComponentBatchMember> members,
    ComponentBatchIssue issue,
    int offendingDebounceMilliseconds = 0)
{
    public string BatchId { get; } = batchId;
    public List<ComponentBatchMember> Members { get; } = members;
    public ComponentBatchIssue Issue { get; } = issue;

    /// <summary>The negative window that triggered <see cref="ComponentBatchIssue.NEGATIVE_DEBOUNCE"/>.</summary>
    public int OffendingDebounceMilliseconds { get; } = offendingDebounceMilliseconds;

    public bool IsValid => Issue == ComponentBatchIssue.NONE;
}

/// <summary>
/// Shared detection logic for <c>[ObservableComponentBatchAsync]</c> batches.
/// <para>
/// The generator uses this to decide what to emit and silently skips anything invalid; the analyzer
/// uses the same results to report RXBG043/RXBG044. Detection lives here exactly once so the two
/// never drift apart - only the diagnostic reporting is analyzer-side.
/// </para>
/// </summary>
public static class ComponentBatchResolver
{
    /// <summary>
    /// Builds the hook method name for a batch id, e.g. <c>search</c> becomes <c>OnSearchBatchChangedAsync</c>.
    /// </summary>
    public static string GetHookMethodName(string batchId)
    {
        var pascalCase = char.ToUpperInvariant(batchId[0]) + batchId.Substring(1);
        return $"On{pascalCase}BatchChangedAsync";
    }

    /// <summary>
    /// A batch id names a generated method, so it has to be a legal C# identifier that is not a keyword.
    /// </summary>
    public static bool IsValidBatchId(string batchId)
    {
        if (string.IsNullOrEmpty(batchId))
        {
            return false;
        }

        return SyntaxFacts.IsValidIdentifier(batchId) &&
               SyntaxFacts.GetKeywordKind(batchId) == SyntaxKind.None;
    }

    /// <summary>
    /// Groups the given property memberships into batches, preserving declaration order.
    /// Members of one batch may declare different debounce windows - that is the point of the
    /// per-property window, not an error.
    /// </summary>
    public static List<ComponentBatchResolution> Resolve(
        IEnumerable<(string PropertyName, string BatchId, int DebounceMilliseconds)> memberships)
    {
        var grouped = new Dictionary<string, List<ComponentBatchMember>>();
        var order = new List<string>();

        foreach (var (propertyName, batchId, debounceMilliseconds) in memberships)
        {
            if (!grouped.TryGetValue(batchId, out var members))
            {
                members = [];
                grouped[batchId] = members;
                order.Add(batchId);
            }

            members.Add(new ComponentBatchMember(propertyName, debounceMilliseconds));
        }

        return order
            .Select(batchId =>
            {
                var members = grouped[batchId];

                if (!IsValidBatchId(batchId))
                {
                    return new ComponentBatchResolution(batchId, members, ComponentBatchIssue.INVALID_BATCH_ID);
                }

                var negative = members.FirstOrDefault(member => member.DebounceMilliseconds < 0);
                if (negative is not null)
                {
                    return new ComponentBatchResolution(
                        batchId, members, ComponentBatchIssue.NEGATIVE_DEBOUNCE, negative.DebounceMilliseconds);
                }

                return new ComponentBatchResolution(batchId, members, ComponentBatchIssue.NONE);
            })
            .ToList();
    }
}
