using BusinessObjects.AI.Models;
using Services.AI.Monitoring;

namespace FCTMS.Tests.Services.AI;

public class AIMetricsCollectorTests
{
    [Fact]
    public void RecordCall_ShouldAggregateTotalsAndProviders()
    {
        // Arrange
        var collector = new AIMetricsCollector();

        // Act
        collector.RecordCall(
            provider: "OpenAI",
            model: "gpt-4o",
            success: true,
            fromCache: false,
            latency: TimeSpan.FromMilliseconds(120),
            usage: new AIUsage { PromptTokens = 10, CompletionTokens = 20, EstimatedCostUsd = 0.002m });

        collector.RecordCall(
            provider: "OpenAI",
            model: "gpt-4o",
            success: false,
            fromCache: false,
            latency: TimeSpan.FromMilliseconds(200),
            usage: null);

        collector.RecordCall(
            provider: "Anthropic",
            model: "claude",
            success: true,
            fromCache: true,
            latency: TimeSpan.Zero,
            usage: new AIUsage { PromptTokens = 5, CompletionTokens = 5, EstimatedCostUsd = 0.001m });

        // Assert
        var summary = collector.GetSummary();

        summary.TotalCalls.Should().Be(3);
        summary.SuccessfulCalls.Should().Be(2);
        summary.FailedCalls.Should().Be(1);
        summary.CacheHits.Should().Be(1);
        summary.TotalTokensUsed.Should().Be(40);
        summary.TotalEstimatedCostUsd.Should().Be(0.003m);
        summary.CallsByProvider["OpenAI"].Should().Be(2);
        summary.CallsByProvider["Anthropic"].Should().Be(1);
        summary.AverageLatencyMs.Should().BeGreaterThan(0);
    }
}
