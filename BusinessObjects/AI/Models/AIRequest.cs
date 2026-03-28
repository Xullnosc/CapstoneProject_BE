namespace BusinessObjects.AI.Models;

/// <summary>
/// Provider-agnostic AI chat request. Build instances via the constructor or object initializer.
/// All provider adapters translate this into their native request format.
/// </summary>
public sealed record AIRequest
{
    /// <summary>Ordered conversation turns. Must contain at least one User message.</summary>
    public required IReadOnlyList<AIMessage> Messages { get; init; }

    /// <summary>Optional system-level instruction prepended to every call.</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>Sampling temperature [0, 2]. Higher = more creative, lower = more deterministic.</summary>
    public float Temperature { get; init; } = 0.7f;

    /// <summary>Upper bound on completion tokens.</summary>
    public int MaxTokens { get; init; } = 1024;

    /// <summary>When true, check Redis cache before calling the provider.</summary>
    public bool UseCache { get; init; } = true;

    /// <summary>Override the default cache TTL for this request. Null = use global default.</summary>
    public int? CacheTtlSeconds { get; init; }

    /// <summary>Caller's user identifier used for per-user rate limiting attribution.</summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Optional user-selected provider override for this request.
    /// When omitted, the configured default provider is used.
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Optional request-scoped provider settings supplied by the authenticated user.
    /// This enables BYOK/BYOA without requiring the server to own third-party secrets.
    /// </summary>
    public AIProviderRequestSettings? ProviderSettings { get; init; }

    public static AIRequest Simple(
        string userPrompt,
        string? systemPrompt = null,
        float temperature = 0.7f,
        int maxTokens = 1024) =>
        new()
        {
            Messages = new[] { new AIMessage(AIMessageRole.User, userPrompt) },
            SystemPrompt = systemPrompt,
            Temperature = temperature,
            MaxTokens = maxTokens
        };
}

public sealed record AIProviderRequestSettings
{
    public string ApiKey { get; init; } = string.Empty;
    public string? Model { get; init; }
    public string? BaseUrl { get; init; }
    public string? ApiVersion { get; init; }
    public string? DeploymentName { get; init; }
    public int? TimeoutSeconds { get; init; }
    public int? MaxRetries { get; init; }
}
