namespace Services.AI.Configuration;

/// <summary>
/// Reads and persists the runtime AI configuration.
/// GET returns the current effective config with masked API keys.
/// PUT writes an override JSON file that is hot-reloaded by IOptionsMonitor.
/// </summary>
public interface IAISettingsService
{
    /// <summary>Returns the current effective AI config with secret fields masked.</summary>
    AIConfigViewDto GetCurrentConfig();

    /// <summary>Saves the provided settings to the override file for hot-reload.</summary>
    Task SaveConfigAsync(UpdateAIConfigRequest request, CancellationToken cancellationToken = default);
}

// ─── View DTO (returned to the client) ───────────────────────────────────────

public sealed class AIConfigViewDto
{
    public bool Enabled { get; init; }
    public string DefaultProvider { get; init; } = string.Empty;
    public string? FallbackProvider { get; init; }
    public Dictionary<string, ProviderViewDto> Providers { get; init; } = new();
    public CacheViewDto Cache { get; init; } = new();
    public RateLimitViewDto RateLimit { get; init; } = new();
}

public sealed class ProviderViewDto
{
    /// <summary>Masked value — never returns the real key to the client.</summary>
    public string ApiKeyMasked { get; init; } = string.Empty;
    public bool HasApiKey { get; init; }
    public string Model { get; init; } = string.Empty;
    public string? BaseUrl { get; init; }
    public string? DeploymentName { get; init; }
    public int TimeoutSeconds { get; init; }
    public int MaxRetries { get; init; }
}

public sealed class CacheViewDto
{
    public bool Enabled { get; init; }
    public int DefaultTtlSeconds { get; init; }
}

public sealed class RateLimitViewDto
{
    public int RequestsPerMinutePerUser { get; init; }
    public int RequestsPerMinuteGlobal { get; init; }
}

// ─── Update DTO (received from the client) ────────────────────────────────────

public sealed class UpdateAIConfigRequest
{
    public bool Enabled { get; init; }
    public string DefaultProvider { get; init; } = string.Empty;
    public string? FallbackProvider { get; init; }
    public Dictionary<string, ProviderUpdateDto> Providers { get; init; } = new();
    public CacheUpdateDto Cache { get; init; } = new();
    public RateLimitUpdateDto RateLimit { get; init; } = new();
}

public sealed class ProviderUpdateDto
{
    /// <summary>Empty string means "keep existing key". A non-empty value replaces it.</summary>
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? BaseUrl { get; init; }
    public string? DeploymentName { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxRetries { get; init; } = 2;
}

public sealed class CacheUpdateDto
{
    public bool Enabled { get; init; } = true;
    public int DefaultTtlSeconds { get; init; } = 3600;
}

public sealed class RateLimitUpdateDto
{
    public int RequestsPerMinutePerUser { get; init; } = 20;
    public int RequestsPerMinuteGlobal { get; init; } = 200;
}
