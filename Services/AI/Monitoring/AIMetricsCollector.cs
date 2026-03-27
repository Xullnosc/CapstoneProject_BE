using System.Collections.Concurrent;
using BusinessObjects.AI.Models;

namespace Services.AI.Monitoring;

/// <summary>
/// Thread-safe in-memory metrics collector for AI calls.
/// Resets on application restart; suitable for operational dashboards.
/// For persistent billing/audit trails, swap this for a DB-backed implementation.
/// </summary>
internal sealed class AIMetricsCollector : IAIMetricsCollector
{
    private long _total;
    private long _success;
    private long _failed;
    private long _cacheHits;
    private long _totalTokens;
    private double _totalLatencyMs;
    private decimal _totalCostUsd;
    private readonly object _costLock = new();

    // Per-provider call counts
    private readonly ConcurrentDictionary<string, long> _byProvider = new();

    public void RecordCall(
        string provider,
        string model,
        bool success,
        bool fromCache,
        TimeSpan latency,
        AIUsage? usage)
    {
        Interlocked.Increment(ref _total);

        if (success)
            Interlocked.Increment(ref _success);
        else
            Interlocked.Increment(ref _failed);

        if (fromCache)
            Interlocked.Increment(ref _cacheHits);

        // Latency accumulation — use Interlocked on a double via bit-swap trick
        double current, updated;
        do
        {
            current = _totalLatencyMs;
            updated = current + latency.TotalMilliseconds;
        } while (Interlocked.CompareExchange(ref _totalLatencyMs, updated, current) != current);

        if (usage is not null)
        {
            Interlocked.Add(ref _totalTokens, usage.TotalTokens);
            lock (_costLock) { _totalCostUsd += usage.EstimatedCostUsd; }
        }

        _byProvider.AddOrUpdate(provider, 1, (_, v) => v + 1);
    }

    public AIMetricsSummary GetSummary()
    {
        var total = Volatile.Read(ref _total);
        var avgLatency = total > 0 ? _totalLatencyMs / total : 0d;
        decimal cost;
        lock (_costLock) { cost = _totalCostUsd; }

        return new AIMetricsSummary
        {
            TotalCalls = total,
            SuccessfulCalls = Volatile.Read(ref _success),
            FailedCalls = Volatile.Read(ref _failed),
            CacheHits = Volatile.Read(ref _cacheHits),
            AverageLatencyMs = Math.Round(avgLatency, 2),
            TotalTokensUsed = Volatile.Read(ref _totalTokens),
            TotalEstimatedCostUsd = cost,
            CallsByProvider = new Dictionary<string, long>(_byProvider)
        };
    }
}
