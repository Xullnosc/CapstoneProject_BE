namespace Services.AI.Configuration;

/// <summary>
/// Root AI configuration. Bound from the "AI" section in appsettings.
/// All provider API keys should be supplied via environment variables (BYOK/BYOA).
/// </summary>
public sealed class AIConfig
{
    public const string SectionKey = "AI";

    /// <summary>Master switch. When false all AI calls return immediately without hitting any provider.</summary>
    public bool Enabled { get; init; } = false;

    public AIProviderType DefaultProvider { get; init; } = AIProviderType.OpenAI;

    /// <summary>Automatically tried when the default provider fails with a retryable error.</summary>
    public AIProviderType? FallbackProvider { get; init; }

    /// <summary>Per-provider settings keyed by <see cref="AIProviderType"/>.</summary>
    public Dictionary<AIProviderType, ProviderSettings> Providers { get; init; } = new();

    public CacheSettings Cache { get; init; } = new();
    public RateLimitSettings RateLimit { get; init; } = new();
}

public sealed class ProviderSettings
{
    /// <summary>BYOK: set via environment variable AI__Providers__OpenAI__ApiKey (or equivalent).</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Model identifier, e.g. "gpt-4o", "claude-3-5-sonnet-20241022", "gemini-1.5-pro".</summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Optional custom base URL.
    /// Required for AzureOpenAI: "https://&lt;resource&gt;.openai.azure.com"
    /// Optional for OpenAI-compatible proxies.
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>Azure OpenAI API version, e.g. "2024-02-01".</summary>
    public string? ApiVersion { get; init; }

    /// <summary>Azure OpenAI deployment name.</summary>
    public string? DeploymentName { get; init; }

    public int TimeoutSeconds { get; init; } = 60;
    public int MaxRetries { get; init; } = 2;
}

public sealed class CacheSettings
{
    public bool Enabled { get; init; } = true;
    /// <summary>Default Redis TTL for cached AI responses.</summary>
    public int DefaultTtlSeconds { get; init; } = 3600;
}

public sealed class RateLimitSettings
{
    /// <summary>Max AI requests per minute per authenticated user. 0 = unlimited.</summary>
    public int RequestsPerMinutePerUser { get; init; } = 20;
    /// <summary>Max AI requests per minute across all users. 0 = unlimited.</summary>
    public int RequestsPerMinuteGlobal { get; init; } = 200;
}
