using BusinessObjects.AI.Models;

namespace Services;

/// <summary>
/// Public AI service. Inject this interface wherever AI functionality is needed.
/// Backed by <see cref="AIService"/> which handles caching, rate limiting, retries,
/// provider fallback, and metrics.
/// </summary>
public interface IAIService
{
    /// <summary>True when AI is enabled via configuration (<c>AI:Enabled=true</c>).</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Full chat API — supports multi-turn conversations, system prompts, and all request options.
    /// </summary>
    Task<AIResponse> ChatAsync(AIRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience wrapper for single-turn generation. Equivalent to calling
    /// <see cref="ChatAsync"/> with a single user message.
    /// </summary>
    Task<string> GenerateAsync(
        string prompt,
        string? systemPrompt = null,
        float temperature = 0.7f,
        int maxTokens = 1024,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns accumulated call metrics (counts, latency, tokens, cost estimates).
    /// Useful for admin dashboards and cost monitoring.
    /// </summary>
    AIMetricsSummary GetMetrics();
}
