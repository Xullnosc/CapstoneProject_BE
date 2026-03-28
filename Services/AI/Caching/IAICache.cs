namespace Services.AI.Caching;

/// <summary>
/// Simple get/set cache abstraction for AI responses.
/// Key is a deterministic hash of the full request parameters.
/// </summary>
internal interface IAICache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);
}
