using BusinessObjects.AI.Models;

namespace Services.AI.Monitoring;

/// <summary>Records observability data for every AI call.</summary>
internal interface IAIMetricsCollector
{
    void RecordCall(string provider, string model, bool success, bool fromCache, TimeSpan latency, AIUsage? usage);
    AIMetricsSummary GetSummary();
}
