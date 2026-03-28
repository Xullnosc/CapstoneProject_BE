using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Services.AI.Caching;

/// <summary>
/// Redis-backed AI response cache. Reuses the existing <see cref="IConnectionMultiplexer"/>
/// registered in the DI container.
/// Key format: <c>ai:cache:{sha256-of-request-json}</c>
/// </summary>
internal sealed class RedisAICache : IAICache
{
    private const string KeyPrefix = "ai:cache:";
    private readonly IRedisService _redisService;
    private readonly ILogger<RedisAICache> _logger;

    public RedisAICache(IRedisService redisService, ILogger<RedisAICache> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var redisKey = BuildKey(key);
        try
        {
            var value = await _redisService.GetValueAsync(redisKey, cancellationToken);
            if (string.IsNullOrEmpty(value))
            {
                _logger.LogDebug("AI cache MISS  {Key}", redisKey);
                return null;
            }
            _logger.LogDebug("AI cache HIT   {Key}", redisKey);
            return value;
        }
        catch (Exception ex)
        {
            // Cache failures are non-fatal — log and proceed without cache
            _logger.LogWarning(ex, "AI cache GET failed for key {Key}. Bypassing cache.", redisKey);
            return null;
        }
    }

    public async Task SetAsync(
        string key,
        string value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    )
    {
        var redisKey = BuildKey(key);
        try
        {
            await _redisService.SetValueAsync(redisKey, value, ttl, cancellationToken);
            _logger.LogDebug("AI cache SET   {Key} TTL={TTL}", redisKey, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AI cache SET failed for key {Key}. Response will not be cached.",
                redisKey
            );
        }
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Produces a stable, 64-char hex key from request parameters so that
    /// two identical requests always resolve to the same cache entry.
    /// </summary>
    public static string BuildCacheKey(
        string provider,
        string model,
        IEnumerable<(string role, string content)> messages,
        float temperature,
        int maxTokens
    )
    {
        var payload = JsonSerializer.Serialize(
            new
            {
                p = provider,
                m = model,
                msgs = messages.Select(x => new { r = x.role, c = x.content }),
                t = temperature,
                mt = maxTokens,
            }
        );
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildKey(string key) => $"{KeyPrefix}{key}";
}
