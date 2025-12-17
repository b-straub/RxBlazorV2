namespace RxBlazorV2Sample.Samples.ServiceModelInteraction;

/// <summary>
/// Service that performs async processing work.
/// This is a pure service - it does NOT know about models or reactive patterns.
/// </summary>
public class ProcessingService
{
    /// <summary>
    /// Simulates async processing of input data.
    /// Returns a ProcessedItem on success.
    /// </summary>
    public async Task<ProcessedItem> ProcessAsync(string input, CancellationToken ct = default)
    {
        // Simulate async work (e.g., API call, database operation)
        await Task.Delay(2000, ct);

        if (input.Equals("error", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new InvalidOperationException("ProcessAsync Error!");
        }
        
        return new ProcessedItem(
            Id: Guid.NewGuid(),
            Input: input,
            Result: $"Processed: {input.ToUpperInvariant()}",
            ProcessedAt: DateTime.Now);
    }
}
