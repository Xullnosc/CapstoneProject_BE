using Services.AI.Configuration;
using BusinessObjects.AI.Models;

namespace Services.AI.Providers;

/// <summary>
/// Contract every AI provider adapter must satisfy.
/// Implementations are <c>internal</c> — callers go through <see cref="IAIService"/> only.
/// </summary>
internal interface IAIProvider
{
    AIProviderType ProviderType { get; }
    string ModelName { get; }
    bool IsConfigured { get; }

    Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default);
}
