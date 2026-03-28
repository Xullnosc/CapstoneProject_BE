using System.Text.Json;
using BusinessObjects.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.AI.Caching;
using Services.AI.Configuration;
using Services.AI.Monitoring;
using Services.AI.Providers;
using Services.AI.RateLimiting;
using Services.AI.Validation;

namespace Services;

/// <summary>
/// Orchestrates AI calls with:
///   • Feature-flag guard (AI:Enabled)
///   • Prompt validation and injection detection
///   • Per-user and global rate limiting (Redis)
///   • Redis response caching with configurable TTL
///   • Exponential-backoff retry on retryable errors
///   • Automatic fallback to a secondary provider
///   • Metrics collection for observability
/// </summary>
internal sealed class AIService : IAIService
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IOptionsMonitor<AIConfig> _configMonitor;
    private AIConfig _config => _configMonitor.CurrentValue;
    private readonly AIProviderFactory _factory;
    private readonly IAICache _cache;
    private readonly IAIRateLimiter _rateLimiter;
    private readonly IAIMetricsCollector _metrics;
    private readonly ILogger<AIService> _logger;

    public bool IsEnabled => _config.Enabled;

    public AIService(
        IOptionsMonitor<AIConfig> config,
        AIProviderFactory factory,
        IAICache cache,
        IAIRateLimiter rateLimiter,
        IAIMetricsCollector metrics,
        ILogger<AIService> logger
    )
    {
        _configMonitor = config;
        _factory = factory;
        _cache = cache;
        _rateLimiter = rateLimiter;
        _metrics = metrics;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<AIResponse> ChatAsync(
        AIRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (!_config.Enabled)
            throw new AIException(
                AIErrorCode.Disabled,
                "AI features are currently disabled.",
                "AIService"
            );

        // 1. Validate input
        PromptValidator.Validate(request);

        // 2. Rate limit
        await EnforceRateLimitAsync(request.UserId, cancellationToken);

        // 3. Cache check
        var provider = _factory.GetDefault(request);
        string? cacheKey = null;

        if (_config.Cache.Enabled && request.UseCache)
        {
            cacheKey = BuildCacheKey(provider, request);
            var cached = await _cache.GetAsync(cacheKey, cancellationToken);
            if (cached is not null)
            {
                var hit = JsonSerializer.Deserialize<AIResponse>(cached, _json);
                if (hit is not null)
                {
                    var cachedResponse = hit with { FromCache = true, Latency = TimeSpan.Zero };
                    _metrics.RecordCall(
                        hit.Provider,
                        hit.Model,
                        success: true,
                        fromCache: true,
                        TimeSpan.Zero,
                        hit.Usage
                    );
                    return cachedResponse;
                }
            }
        }

        // 4. Call provider (with retry + fallback)
        var response = await CallWithRetryAndFallbackAsync(provider, request, cancellationToken);

        // 5. Store in cache
        if (_config.Cache.Enabled && request.UseCache && cacheKey is not null)
        {
            var ttl = TimeSpan.FromSeconds(
                request.CacheTtlSeconds ?? _config.Cache.DefaultTtlSeconds
            );
            await _cache.SetAsync(
                cacheKey,
                JsonSerializer.Serialize(response, _json),
                ttl,
                cancellationToken
            );
        }

        _metrics.RecordCall(
            response.Provider,
            response.Model,
            success: true,
            fromCache: false,
            response.Latency,
            response.Usage
        );
        return response;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        string? systemPrompt = null,
        float temperature = 0.7f,
        int maxTokens = 1024,
        string? userId = null,
        CancellationToken cancellationToken = default
    )
    {
        var request = new AIRequest
        {
            Messages = new[] { new AIMessage(AIMessageRole.User, prompt) },
            SystemPrompt = systemPrompt,
            Temperature = temperature,
            MaxTokens = maxTokens,
            UserId = userId,
        };

        var response = await ChatAsync(request, cancellationToken);
        return response.Content;
    }

    public AIMetricsSummary GetMetrics() => _metrics.GetSummary();

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<AIResponse> CallWithRetryAndFallbackAsync(
        IAIProvider provider,
        AIRequest request,
        CancellationToken cancellationToken
    )
    {
        var maxRetries = GetMaxRetries(provider);

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await provider.CallAsync(request, cancellationToken);
            }
            catch (AIException ex) when (ex.IsRetryable && attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 0.5); // 0.5s, 1s, 2s
                _logger.LogWarning(
                    "AI call to {Provider} failed (attempt {Attempt}/{Max}): {Message}. Retrying in {Delay}s.",
                    ex.ProviderName,
                    attempt + 1,
                    maxRetries + 1,
                    ex.Message,
                    delay.TotalSeconds
                );

                await Task.Delay(delay, cancellationToken);
            }
            catch (AIException ex)
            {
                _metrics.RecordCall(
                    ex.ProviderName,
                    provider.ModelName,
                    success: false,
                    fromCache: false,
                    TimeSpan.Zero,
                    null
                );

                // Try fallback provider on non-recoverable primary failure
                var fallback = _factory.TryGetFallback(request);
                if (fallback is not null && fallback.ProviderType != provider.ProviderType)
                {
                    _logger.LogWarning(
                        "Primary AI provider '{Primary}' failed: {Message}. Trying fallback '{Fallback}'.",
                        ex.ProviderName,
                        ex.Message,
                        fallback.ProviderType
                    );

                    return await CallWithRetryAndFallbackAsync(
                        fallback,
                        request with
                        {
                            UseCache = false,
                        },
                        cancellationToken
                    );
                }

                throw;
            }
        }

        // Exhausted retries
        throw new AIException(
            AIErrorCode.ProviderUnavailable,
            $"AI provider '{provider.ProviderType}' failed after {maxRetries + 1} attempt(s).",
            provider.ProviderType.ToString()
        );
    }

    private async Task EnforceRateLimitAsync(string? userId, CancellationToken cancellationToken)
    {
        var result = await _rateLimiter.CheckAsync(userId, cancellationToken);
        if (!result.IsAllowed)
        {
            _metrics.RecordCall(
                "RateLimiter",
                "n/a",
                success: false,
                fromCache: false,
                TimeSpan.Zero,
                null
            );
            throw new AIException(AIErrorCode.RateLimited, result.Reason, "RateLimiter");
        }
    }

    private static string BuildCacheKey(IAIProvider provider, AIRequest request)
    {
        var messageTuples = request.Messages.Select(m =>
            (m.Role.ToString().ToLowerInvariant(), m.Content)
        );

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messageTuples = messageTuples.Prepend(("system", request.SystemPrompt));

        return RedisAICache.BuildCacheKey(
            provider.ProviderType.ToString(),
            provider.ModelName,
            messageTuples,
            request.Temperature,
            request.MaxTokens
        );
    }

    private static int GetMaxRetries(IAIProvider provider)
    {
        // Provider settings are not exposed here; use a safe default.
        // The factory holds the settings per provider.
        return 2;
    }
}
