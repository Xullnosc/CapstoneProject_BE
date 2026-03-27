using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.AI.Configuration;
using StackExchange.Redis;

namespace Services.AI.RateLimiting;

/// <summary>
/// Redis-backed sliding-window rate limiter.
/// Uses a sorted set per bucket: members are call timestamps (ticks),
/// scores are Unix seconds. Each minute's window is maintained atomically.
///
/// Key format:
///   Global : <c>ai:rl:global:{unix-minute}</c>
///   Per-user: <c>ai:rl:user:{userId}:{unix-minute}</c>
/// </summary>
internal sealed class RedisRateLimiter : IAIRateLimiter
{
    private readonly IDatabase _db;
    private readonly RateLimitSettings _settings;
    private readonly ILogger<RedisRateLimiter> _logger;

    public RedisRateLimiter(
        IConnectionMultiplexer redis,
        IOptions<AIConfig> config,
        ILogger<RedisRateLimiter> logger)
    {
        _db = redis.GetDatabase();
        _settings = config.Value.RateLimit;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddMinutes(-1);
        var score = (double)now.ToUnixTimeMilliseconds();
        var member = now.Ticks.ToString();
        var expiry = TimeSpan.FromMinutes(2); // TTL slightly longer than window

        try
        {
            // ── Global check ──────────────────────────────────────────────────
            if (_settings.RequestsPerMinuteGlobal > 0)
            {
                var globalKey = $"ai:rl:global";
                var globalCount = await SlidingWindowCountAsync(
                    globalKey, member, score, windowStart, expiry);

                if (globalCount > _settings.RequestsPerMinuteGlobal)
                {
                    _logger.LogWarning("AI global rate limit exceeded ({Count}/{Max})", globalCount, _settings.RequestsPerMinuteGlobal);
                    return Denied("Global AI rate limit exceeded.", retryAfter: 60);
                }
            }

            // ── Per-user check ────────────────────────────────────────────────
            if (_settings.RequestsPerMinutePerUser > 0 && !string.IsNullOrWhiteSpace(userId))
            {
                var userKey = $"ai:rl:user:{userId}";
                var userCount = await SlidingWindowCountAsync(
                    userKey, member, score, windowStart, expiry);

                if (userCount > _settings.RequestsPerMinutePerUser)
                {
                    _logger.LogWarning("AI per-user rate limit exceeded for User={UserId} ({Count}/{Max})",
                        userId, userCount, _settings.RequestsPerMinutePerUser);
                    return Denied($"Rate limit exceeded. You may make {_settings.RequestsPerMinutePerUser} AI requests per minute.", retryAfter: 60);
                }
            }

            return new RateLimitResult { IsAllowed = true };
        }
        catch (RedisException ex)
        {
            // Redis failure → allow the call and log. Prefer availability over strict limiting.
            _logger.LogError(ex, "Redis rate-limit check failed. Allowing request through.");
            return new RateLimitResult { IsAllowed = true };
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<long> SlidingWindowCountAsync(
        string key,
        string member,
        double score,
        DateTimeOffset windowStart,
        TimeSpan expiry)
    {
        var tran = _db.CreateTransaction();

        // Remove entries outside the 1-minute window
        _ = tran.SortedSetRemoveRangeByScoreAsync(key, 0, (double)windowStart.ToUnixTimeMilliseconds());

        // Add current request
        _ = tran.SortedSetAddAsync(key, member, score);

        // Refresh TTL
        _ = tran.KeyExpireAsync(key, expiry);

        // Count entries in window
        var countTask = tran.SortedSetLengthAsync(key);

        await tran.ExecuteAsync();

        return await countTask;
    }

    private static RateLimitResult Denied(string reason, int retryAfter) =>
        new() { IsAllowed = false, Reason = reason, RetryAfterSeconds = retryAfter };
}
