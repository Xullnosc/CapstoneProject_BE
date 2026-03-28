using BusinessObjects.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.AI.Caching;
using Services.AI.Configuration;
using Services.AI.Monitoring;
using Services.AI.Providers;
using Services.AI.RateLimiting;

namespace FCTMS.Tests.Services.AI;

public class AIServiceTests
{
    [Fact]
    public async Task ChatAsync_ShouldThrowDisabled_WhenAiFeatureFlagOff()
    {
        // Arrange
        var configMonitor = Mock.Of<IOptionsMonitor<AIConfig>>(m =>
            m.CurrentValue == new AIConfig { Enabled = false });

        // Provider factory should never be used when AI is disabled; pass null-forgiving placeholder.
        global::Services.AI.Providers.AIProviderFactory providerFactory = null!;

        var cache = new NoopCache();
        var limiter = new NoopLimiter();
        var metrics = new NoopMetricsCollector();
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<AIService>();

        var service = new AIService(
            configMonitor,
            providerFactory,
            cache,
            limiter,
            metrics,
            logger);

        var request = new AIRequest
        {
            Messages = new[] { new AIMessage(AIMessageRole.User, "hello") }
        };

        // Act
        Func<Task> act = async () => await service.ChatAsync(request);

        // Assert
        await act.Should().ThrowAsync<AIException>()
            .Where(e => e.Code == AIErrorCode.Disabled);
    }

    private sealed class NoopCache : IAICache
    {
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoopLimiter : IAIRateLimiter
    {
        public Task<RateLimitResult> CheckAsync(string? userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new RateLimitResult { IsAllowed = true });
    }

    private sealed class NoopMetricsCollector : IAIMetricsCollector
    {
        public void RecordCall(string provider, string model, bool success, bool fromCache, TimeSpan latency, AIUsage? usage)
        {
        }

        public AIMetricsSummary GetSummary() => new();
    }
}
