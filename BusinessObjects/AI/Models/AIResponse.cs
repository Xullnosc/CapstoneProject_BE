namespace BusinessObjects.AI.Models;

/// <summary>Unified AI response returned by all provider adapters and the AIService orchestrator.</summary>
public sealed record AIResponse
{
    /// <summary>Text content produced by the model.</summary>
    public required string Content { get; init; }

    /// <summary>Token usage reported by the provider.</summary>
    public required AIUsage Usage { get; init; }

    /// <summary>Name of the provider that served this response, e.g. OpenAI.</summary>
    public required string Provider { get; init; }

    /// <summary>Model identifier returned by the provider, e.g. gpt-4o.</summary>
    public required string Model { get; init; }

    /// <summary>True when this response was retrieved from Redis rather than calling the provider.</summary>
    public bool FromCache { get; init; }

    /// <summary>End-to-end latency including any retry attempts.</summary>
    public TimeSpan Latency { get; init; }
}
