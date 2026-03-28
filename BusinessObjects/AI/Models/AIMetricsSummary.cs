namespace BusinessObjects.AI.Models;

/// <summary>Summary snapshot returned by GET /api/ai/metrics.</summary>
public sealed class AIMetricsSummary
{
    public long TotalCalls { get; init; }
    public long SuccessfulCalls { get; init; }
    public long FailedCalls { get; init; }
    public long CacheHits { get; init; }
    public double AverageLatencyMs { get; init; }
    public long TotalTokensUsed { get; init; }
    public decimal TotalEstimatedCostUsd { get; init; }
    public IReadOnlyDictionary<string, long> CallsByProvider { get; init; } = new Dictionary<string, long>();
}
