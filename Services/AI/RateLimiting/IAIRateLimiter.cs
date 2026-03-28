namespace Services.AI.RateLimiting;

/// <summary>
/// Sliding-window rate limiter for AI calls.
/// Returns true when the caller is within quota, false when limit is exceeded.
/// </summary>
internal interface IAIRateLimiter
{
    /// <param name="userId">Authenticated user identifier. Null / empty = anonymous bucket.</param>
    Task<RateLimitResult> CheckAsync(string? userId, CancellationToken cancellationToken = default);
}

public readonly struct RateLimitResult
{
    public bool IsAllowed { get; init; }
    /// <summary>Seconds until the sliding window resets for this caller.</summary>
    public int RetryAfterSeconds { get; init; }
    public string Reason { get; init; }
}
